using System.CommandLine;
using dotenv.net;
using Microsoft.Extensions.DependencyInjection;
using Resolver.Athena;
using Resolver.Athena.Auth;
using Resolver.Athena.DependencyInjection;
using Resolver.Athena.Images;
using Resolver.Athena.Interfaces;
using Resolver.Athena.LowLevel;


static class Program
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

    private static readonly Argument<List<string>> s_imagePathsArgument = new("image-paths")
    {
        Description = "One or more paths to image files to classify",
    };

    private static readonly Argument<string> s_deploymentIdArgument = new("deployment-id")
    {
        Description = "Deployment ID to use for classification",
    };

    public static async Task Main(string[] args)
    {
        var rootCommand = new RootCommand(Description);

        var dotenvPathOption = new Option<string>("--dotenv", "-d")
        {
            Description = "Path to .env file containing configuration",
            DefaultValueFactory = _ => ".env",
            Recursive = true,
        };
        rootCommand.Options.Add(dotenvPathOption);

        rootCommand.AddAthenaCommand("token-test", "Test OAuth token retrieval", DoTokenTestCommand);
        rootCommand.AddAthenaCommand("list-deployments", "List deployments from Athena", DoListDeploymentsCommand);
        var classifySingleCommand = rootCommand.AddAthenaCommand("classify-single", "Classify a single image", DoClassifySingleCommand);
        classifySingleCommand.Arguments.Add(s_imagePathsArgument);

        var classifyStreamingCommand = rootCommand.AddAthenaCommand("classify-streaming", "Classify images using streaming with batching", DoClassifyStreamingCommand);
        classifyStreamingCommand.Arguments.Add(s_deploymentIdArgument);
        classifyStreamingCommand.Arguments.Add(s_imagePathsArgument);

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
        var imagePaths = parseResult.GetValue(s_imagePathsArgument) ?? throw new InvalidOperationException("Image path argument is required.");

        var failureCount = 0;

        foreach (var imagePath in imagePaths)
        {
            try
            {
                var imageData = await File.ReadAllBytesAsync(imagePath, cancellationToken);
                var image = new AthenaImageEncoded(imageData);

                var result = await athenaClient.ClassifySingleImageAsync(image, cancellationToken);

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
            }
            catch (Exception ex)
            {
                failureCount++;
                Console.WriteLine($"Failed to classify image: {ex.Message}");
            }
        }
        if (failureCount == 0)
        {
            Console.WriteLine("All images classified successfully.");
            return 0;
        }

        Console.WriteLine($"{failureCount} image(s) failed to classify.");
        return 1;
    }

    public static async Task<int> DoClassifyStreamingCommand(ParseResult parseResult, CancellationToken cancellationToken)
    {
        LoadDotEnv(parseResult);
        var svcs = new ServiceCollection()
            .AddBatchingLowLevelStreamingClient(ConfigureBatchingLowLevelStreamingClientFromEnv(parseResult), ConfigureOAuthTokenManagerFromEnv)
            .BuildServiceProvider();

        var streamingClient = svcs.GetRequiredService<IBatchingLowLevelStreamingClient>();
        await streamingClient.StartAsync(cancellationToken);

        var imagePaths = parseResult.GetValue(s_imagePathsArgument) ?? throw new InvalidOperationException("Image path argument is required.");

        await Parallel.ForEachAsync(imagePaths, async (imagePath, ct) =>
        {
            try
            {
                var imageData = await File.ReadAllBytesAsync(imagePath, ct);
                var image = new AthenaImageEncoded(imageData);

                await streamingClient.SendAsync(image, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to classify image: {ex.Message}");
            }

        });

        Console.WriteLine("All images sent, awaiting results...");

        await foreach (var response in streamingClient.GetResponsesAsync(cancellationToken))
        {
            Console.WriteLine("Received a response batch:");
            foreach (var result in response.Outputs)
            {
                if (result.Error != null)
                {
                    Console.WriteLine($"Error: {result.Error.Code} - {result.Error.Message}");
                    continue;
                }

                Console.WriteLine($"Classification Results for Correlation ID: {result.CorrelationId}");
                foreach (var classification in result.Classifications)
                {
                    Console.WriteLine($"- {classification.Label}: {classification.Weight}");
                }
            }
        }

        return 0;
    }

    private static Action<BatchingLowLevelStreamingClientConfiguration> ConfigureBatchingLowLevelStreamingClientFromEnv(ParseResult parseResult) => options =>
    {
        options.DeploymentId = parseResult.GetValue(s_deploymentIdArgument) ?? throw new InvalidOperationException("Deployment ID argument is required.");
        options.SendMd5Hash = true;
        options.SendSha1Hash = true;

        var endpoint = Environment.GetEnvironmentVariable("ATHENA_ENDPOINT") ?? throw new InvalidOperationException("ATHENA_ENDPOINT not set in environment variables.");
        options.Endpoint = endpoint;

        var affiliate = Environment.GetEnvironmentVariable("ATHENA_AFFILIATE") ?? throw new InvalidOperationException("ATHENA_AFFILIATE not set in environment variables.");
        options.Affiliate = affiliate;

        if (Environment.GetEnvironmentVariable("ATHENA_BATCH_SIZE") is string batchMaxSizeStr && int.TryParse(batchMaxSizeStr, out var batchMaxSize))
        {
            options.MaxBatchSize = batchMaxSize;
        }
        else
        {
            options.MaxBatchSize = 16; // Default value
        }

        if (Environment.GetEnvironmentVariable("ATHENA_CHANNEL_CAPACITY") is string channelCapacityStr && int.TryParse(channelCapacityStr, out var channelCapacity))
        {
            options.ChannelCapacity = channelCapacity;
        }
        else
        {
            options.ChannelCapacity = 100; // Default value
        }
    };

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

    private static Action<AthenaClientConfiguration> ConfigureAthenaClientFromEnv => options =>
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
}
