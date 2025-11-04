using System.CommandLine;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;
using dotenv.net;
using Microsoft.Extensions.DependencyInjection;
using Resolver.AthenaApiClient;
using Resolver.AthenaApiClient.Auth;
using Resolver.AthenaApiClient.DependencyInjection;
using Resolver.AthenaApiClient.Interfaces;
using Resolver.AthenaClient.DependencyInjection;
using Resolver.AthenaClient.Images;
using Resolver.AthenaClient.Interfaces;
using Resolver.AthenaClient.Models;
using Resolver.AthenaDataflowClient.DependencyInjection;
using Resolver.AthenaDataflowClient.Interfaces;


static partial class Program
{
    public const string Description = """
Athena Simple CLI
-----------------
A simple command-line interface to illustrate the usage of the Athena client library.

Setup:
Create a `.env` file in the project root with the following content:
   OAUTH_CLIENT_ID=your_client_id
   OAUTH_CLIENT_SECRET=your_client_secret
   ATHENA_ENDPOINT=your-athena-endpoint.com

You can optionally set these environment variables to override these provided
defaults:
   OAUTH_AUDIENCE=crisp-athena-live
   OAUTH_AUTH_URL=https://crispthinking.auth0.com/oauth/token
""";

    private static readonly Option<string> s_dotenvPathOption = new("--dotenv", "-d")
    {
        Description = "Path to .env file containing configuration",
        DefaultValueFactory = _ => ".env",
        Recursive = true,
    };

    private static readonly Argument<string> s_imagePathArgument = new("image-path")
    {
        Description = "Path to the image file to classify",
    };

    private static readonly Argument<string> s_deploymentIdArgument = new("deployment-id")
    {
        Description = "Deployment ID to target for streaming classify",
    };

    private static readonly Option<int> s_repeatOption = new("--repeat", "-r")
    {
        Description = "If set to >0, repeat sending images every N seconds until cancelled",
        DefaultValueFactory = _ => 0,
    };

    public static async Task Main(string[] args)
    {
        var rootCommand = new RootCommand(Description);

        // reuse the pre-defined static option so LoadDotEnv can access the same option
        rootCommand.Options.Add(s_dotenvPathOption);

        rootCommand.AddAthenaCommand("token-test", "Test OAuth token retrieval", DoTokenTestCommand);
        rootCommand.AddAthenaCommand("list-deployments", "List deployments from Athena", DoListDeploymentsCommand);
        var classifySingleCommand = rootCommand.AddAthenaCommand("classify-single", "Classify a single image ", DoClassifySingleCommand);
        classifySingleCommand.Arguments.Add(s_imagePathArgument);
        var classifyStreamCommand = rootCommand.AddAthenaCommand("classify", "Classify images using the standard IAsyncEnumerable client", DoClassifyCommand);
        classifyStreamCommand.Options.Add(s_repeatOption);
        classifyStreamCommand.Arguments.Add(s_deploymentIdArgument);
        classifyStreamCommand.Arguments.Add(s_imagePathArgument);

        var tplClassifyStreamCommand = rootCommand.AddAthenaCommand("classify-dataflow", "Classify images using the TPL Dataflow Client", DoClassifyDataflowCommand);
        tplClassifyStreamCommand.Options.Add(s_repeatOption);
        tplClassifyStreamCommand.Arguments.Add(s_deploymentIdArgument);
        tplClassifyStreamCommand.Arguments.Add(s_imagePathArgument);

        var parseResult = rootCommand.Parse(args);
        await parseResult.InvokeAsync();
    }

    public static async Task<int> DoTokenTestCommand(ParseResult parseResult, CancellationToken cancellationToken)
    {
        LoadDotEnv(parseResult);
        var serviceProvider = new ServiceCollection()
            .AddOAuthTokenManager(ConfigureOAuthTokenManagerFromEnv)
            .BuildServiceProvider();

        var tokenManager = serviceProvider.GetRequiredService<ITokenManager>();

        try
        {
            var token = await tokenManager.GetTokenAsync(cancellationToken);
            Console.WriteLine("Successfully obtained OAuth Token.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to obtain OAuth Token: {ex.Message}");
            return 1;
        }
    }

    public static async Task<int> DoListDeploymentsCommand(ParseResult parseResult, CancellationToken cancellationToken)
    {
        LoadDotEnv(parseResult);

        var svcs = new ServiceCollection()
            .AddAthenaClient(ConfigureAthenaClientFromEnv, ConfigureOAuthTokenManagerFromEnv)
            .BuildServiceProvider();

        var athenaClient = svcs.GetRequiredService<IAthenaClient>();

        try
        {
            var deployments = await athenaClient.ListDeploymentsAsync(cancellationToken);

            if (deployments.Count == 0)
            {
                Console.WriteLine("No deployments found.");
                return 0;
            }

            Console.WriteLine("Deployments:");
            foreach (var deployment in deployments)
            {
                Console.WriteLine($"- {deployment.DeploymentId} (Backlog: {deployment.Backlog})");
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to list deployments: {ex.Message}");
            return 1;
        }
    }

    public static async Task<int> DoClassifySingleCommand(ParseResult parseResult, CancellationToken cancellationToken)
    {
        LoadDotEnv(parseResult);
        var svcs = new ServiceCollection()
            .AddAthenaClient(ConfigureAthenaClientFromEnv, ConfigureOAuthTokenManagerFromEnv)
            .BuildServiceProvider();

        var athenaClient = svcs.GetRequiredService<IAthenaClient>();
        var imagePath = parseResult.GetValue(s_imagePathArgument) ?? throw new InvalidOperationException("Image path argument is required.");

        try
        {
            var imageData = await File.ReadAllBytesAsync(imagePath, cancellationToken);
            var image = new AthenaImageEncoded(imageData);

            var result = await athenaClient.ClassifySingleAsync(image, cancellationToken);

            if (result.ErrorDetails != null)
            {
                Console.WriteLine($"Error: {result.ErrorDetails.Code} - {result.ErrorDetails.Message}");
                return 1;
            }

            Console.WriteLine($"Classification Results for Correlation ID: {result.CorrelationId}");
            foreach (var classification in result.Classifications)
            {
                Console.WriteLine($"- {classification.Label}: {classification.Confidence}");
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to classify image: {ex.Message}");
            return 1;
        }
    }

    public static async Task<int> DoClassifyCommand(ParseResult parseResult, CancellationToken cancellationToken)
    {
        LoadDotEnv(parseResult);

        var svcs = new ServiceCollection()
            .AddAthenaClient(ConfigureAthenaClientFromEnv, ConfigureOAuthTokenManagerFromEnv)
            .BuildServiceProvider();

        var athenaClient = svcs.GetRequiredService<IAthenaClient>();
        var deploymentId = GetDeploymentId(parseResult);
        var path = parseResult.GetValue(s_imagePathArgument) ?? throw new InvalidOperationException("Image path argument is required.");

        var repeatSeconds = parseResult.GetValue(s_repeatOption);

        try
        {
            // gather files: if a directory is provided, enumerate common image extensions; otherwise treat as single file
            var files = GatherImagePaths(path);

            var requestsToSend = files.Count;

            async IAsyncEnumerable<ClassificationRequest> GenerateRequests([EnumeratorCancellation] CancellationToken ct)
            {
                while (!ct.IsCancellationRequested)
                {
                    foreach (var file in files)
                    {
                        ct.ThrowIfCancellationRequested();

                        byte[] data;
                        try
                        {
                            data = await File.ReadAllBytesAsync(file, ct).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to read file {file}: {ex.Message}");
                            continue;
                        }

                        var image = new AthenaImageEncoded(data);
                        var rawCorrelation = Path.GetFileNameWithoutExtension(file) ?? string.Empty;
                        var sanitized = SanitizeCorrelationId(rawCorrelation);

                        Console.WriteLine($"Sending file: {file} (correlation: {sanitized})");

                        yield return new ClassificationRequest(deploymentId, image, sanitized);
                    }

                    if (repeatSeconds <= 0)
                    {
                        yield break;
                    }

                    Console.WriteLine($"Waiting {repeatSeconds} seconds before next cycle...");
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(repeatSeconds), ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        yield break;
                    }
                }
            }

            if (repeatSeconds > 0)
            {
                Console.WriteLine($"Repeating every {repeatSeconds} seconds. Press Ctrl+C to stop.");
            }

            var consumedResponses = 0;
            var errorResponses = 0;

            Console.WriteLine("[consumer] starting streaming consume");
            try
            {
                await foreach (var result in athenaClient.ClassifyAsync(GenerateRequests(cancellationToken), cancellationToken))
                {
                    consumedResponses++;
                    if (result.ErrorDetails != null)
                    {
                        errorResponses++;
                        Console.WriteLine($"Error: {result.ErrorDetails.Code} - {result.ErrorDetails.Message}");
                        continue;
                    }


                    Console.WriteLine($"Classification Results for Correlation ID: {result.CorrelationId}");
                    if (result.Classifications == null || result.Classifications.Count == 0)
                    {
                        Console.WriteLine("- <no classifications returned>");
                    }
                    else
                    {
                        foreach (var classification in result.Classifications)
                        {
                            Console.WriteLine($"- {classification.Label}: {classification.Confidence}");
                        }
                    }

                    if (repeatSeconds == 0 && consumedResponses >= requestsToSend)
                    {
                        Console.WriteLine("[consume] All requests processed, ending stream.");
                        break;
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[consumer] streaming exception: {ex.GetType().FullName} - {ex.Message}");
                Console.WriteLine(ex);
            }
            finally
            {
                Console.WriteLine("[consumer] streaming ended");
            }

            Console.WriteLine("Stream Summary:");
            Console.WriteLine($"- Total Requests Sent: {requestsToSend}");
            Console.WriteLine($"- Total Responses Consumed: {consumedResponses}");
            Console.WriteLine($"- Total Successful Responses: {consumedResponses - errorResponses}");
            Console.WriteLine($"- Total Error Responses: {errorResponses}");
            Console.WriteLine($"- Error Rate: {(consumedResponses > 0 ? (double)errorResponses / consumedResponses * 100.0 : 0.0):F2}%");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to classify images (stream): {ex.Message}");
            return 1;
        }
    }

    public static async Task<int> DoClassifyDataflowCommand(ParseResult parseResult, CancellationToken cancellationToken)
    {
        LoadDotEnv(parseResult);

        var svcs = new ServiceCollection()
            .AddAthenaDataflowClient(ConfigureAthenaClientFromEnv, ConfigureOAuthTokenManagerFromEnv)
            .BuildServiceProvider();

        var athenaClient = svcs.GetRequiredService<IAthenaDataflowClient>();
        var pipeline = await athenaClient.CreatePipelineAsync(cancellationToken);
        var deploymentId = GetDeploymentId(parseResult);
        var path = parseResult.GetValue(s_imagePathArgument) ?? throw new InvalidOperationException("Image path argument is required.");

        var repeatSeconds = parseResult.GetValue(s_repeatOption);

        try
        {
            // gather files: if a directory is provided, enumerate common image extensions; otherwise treat as single file
            var blockLinkOptions = new DataflowLinkOptions
            {
                PropagateCompletion = true,
            };

            var requestsToSendCount = new TaskCompletionSource<int>();

            var filepathGathererBlock = new TransformManyBlock<string, string>(dirPath =>
            {
                var files = GatherImagePaths(path);
                requestsToSendCount.TrySetResult(files.Count);
                return files;

            }, new ExecutionDataflowBlockOptions
            {
                CancellationToken = cancellationToken,
            });

            var loaderBlock = new TransformBlock<string, ClassificationRequest>(async f =>
            {
                var data = await File.ReadAllBytesAsync(f, cancellationToken).ConfigureAwait(false);
                var image = new AthenaImageEncoded(data);
                var rawCorrelation = Path.GetFileNameWithoutExtension(f) ?? string.Empty;
                var sanitized = SanitizeCorrelationId(rawCorrelation);

                Console.WriteLine($"Sending file: {f} (correlation: {sanitized})");

                return new ClassificationRequest(deploymentId, image, sanitized);
            }, new ExecutionDataflowBlockOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount,
            });
            filepathGathererBlock.LinkTo(loaderBlock, blockLinkOptions);


            loaderBlock.LinkTo(pipeline.Input, blockLinkOptions);

            var consumedResponses = 0;
            var errorResponses = 0;

            var pipelineTCS = new TaskCompletionSource();

            var loggerBlock = new ActionBlock<ClassificationResult>(async result =>
            {
                Console.WriteLine("[consumer] received classification result");
                consumedResponses++;


                if (result.ErrorDetails != null)
                {
                    errorResponses++;
                    Console.WriteLine($"Error: {result.ErrorDetails.Code} - {result.ErrorDetails.Message}");
                    if (repeatSeconds == 0 && consumedResponses >= await requestsToSendCount.Task)
                    {
                        pipelineTCS.TrySetResult();
                    }
                    return;
                }


                Console.WriteLine($"Classification Results for Correlation ID: {result.CorrelationId}");
                if (result.Classifications == null || result.Classifications.Count == 0)
                {
                    Console.WriteLine("- <no classifications returned>");
                }
                else
                {
                    foreach (var classification in result.Classifications)
                    {
                        Console.WriteLine($"- {classification.Label}: {classification.Confidence}");
                    }
                }

                if (repeatSeconds == 0 && consumedResponses >= await requestsToSendCount.Task)
                {
                    pipelineTCS.TrySetResult();
                }
            }, new ExecutionDataflowBlockOptions
            {
                CancellationToken = cancellationToken,
            });

            pipeline.Output.LinkTo(loggerBlock, blockLinkOptions);

            if (repeatSeconds > 0)
            {
                Console.WriteLine($"Repeating every {repeatSeconds} seconds. Press Ctrl+C to stop.");
                while (!cancellationToken.IsCancellationRequested)
                {
                    filepathGathererBlock.Post(path);

                    Console.WriteLine($"Waiting {repeatSeconds} seconds before next cycle...");
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(repeatSeconds), cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            else
            {
                filepathGathererBlock.Post(path);
            }

            Console.WriteLine("[consumer] starting dataflow consume");

            await pipelineTCS.Task;

            Console.WriteLine("Dataflow Summary:");
            Console.WriteLine($"- Total Requests Sent: {await requestsToSendCount.Task}");
            Console.WriteLine($"- Total Responses Consumed: {consumedResponses}");
            Console.WriteLine($"- Total Successful Responses: {consumedResponses - errorResponses}");
            Console.WriteLine($"- Total Error Responses: {errorResponses}");
            Console.WriteLine($"- Error Rate: {(consumedResponses > 0 ? (double)errorResponses / consumedResponses * 100.0 : 0.0):F2}%");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to classify images (stream): {ex.Message}");
            return 1;
        }
    }

    private static List<string> GatherImagePaths(string path)
    {
        var files = new List<string>();
        if (Directory.Exists(path))
        {
            var exts = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp" };
            files.AddRange(Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly)
                .Where(f => exts.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase)));
        }
        else if (File.Exists(path))
        {
            files.Add(path);
        }
        else
        {
            throw new InvalidOperationException($"Provided path does not exist: {path}");
        }

        if (files.Count == 0)
        {
            throw new InvalidOperationException("No image files found at the provided path.");
        }

        return files;
    }

    private static string SanitizeCorrelationId(string raw)
    {
        var sanitized = CorrelationIdRegex().Replace(raw, "_");
        return string.IsNullOrWhiteSpace(sanitized)
            ? Guid.NewGuid().ToString("N")
            : sanitized;
    }

    private static Action<OAuthTokenManagerConfiguration> ConfigureOAuthTokenManagerFromEnv => options =>
    {
        options.ClientId = Environment.GetEnvironmentVariable("OAUTH_CLIENT_ID") ?? throw new InvalidOperationException("OAUTH_CLIENT_ID not set in environment variables.");
        options.ClientSecret = Environment.GetEnvironmentVariable("OAUTH_CLIENT_SECRET") ?? throw new InvalidOperationException("OAUTH_CLIENT_SECRET not set in environment variables.");

        if (Environment.GetEnvironmentVariable("OAUTH_AUDIENCE") is string audience)
        {
            options.Audience = audience;
        }

        if (Environment.GetEnvironmentVariable("OAUTH_AUTH_URL") is string authUrl)
        {
            options.AuthUrl = authUrl;
        }
    };

    private static Action<AthenaApiClientConfiguration> ConfigureAthenaClientFromEnv => options =>
    {
        var endpoint = Environment.GetEnvironmentVariable("ATHENA_ENDPOINT") ?? throw new InvalidOperationException("ATHENA_ENDPOINT not set in environment variables.");
        options.Endpoint = endpoint;
        var affiliate = Environment.GetEnvironmentVariable("ATHENA_AFFILIATE") ?? throw new InvalidOperationException("ATHENA_AFFILIATE not set in environment variables.");
        options.Affiliate = affiliate;
        options.SendMd5Hash = true;
        options.SendSha1Hash = true;
    };

    private static void LoadDotEnv(ParseResult parseResult)
    {
        var dotenvPath = parseResult.GetValue(s_dotenvPathOption) ?? ".env";
        if (File.Exists(dotenvPath))
        {
            DotEnv.Fluent()
                .WithEnvFiles(dotenvPath)
                .Load();

            Console.WriteLine($"Loaded environment variables from {dotenvPath}");
        }
        else
        {
            Console.WriteLine($"No .env file found at {dotenvPath}, proceeding with existing environment variables.");
        }
    }

    private static string GetDeploymentId(ParseResult parseResult)
    {
        var deploymentId = parseResult.GetValue(s_deploymentIdArgument) ?? throw new InvalidOperationException("deployment-id argument is required.");
        if (deploymentId.Equals("random", StringComparison.OrdinalIgnoreCase))
        {
            deploymentId = Guid.NewGuid().ToString("N");
        }
        return deploymentId;
    }

    public static Command AddAthenaCommand(this Command parentCommand, string name, string description, Func<ParseResult, CancellationToken, Task<int>> commandHandler)
    {
        var command = new Command(name, description);
        command.SetAction((parseResult, cancellationToken) =>
        {
            return commandHandler(parseResult, cancellationToken);
        });
        parentCommand.Subcommands.Add(command);
        return command;
    }

    [GeneratedRegex("[^A-Za-z0-9_-]")]
    private static partial Regex CorrelationIdRegex();
}
