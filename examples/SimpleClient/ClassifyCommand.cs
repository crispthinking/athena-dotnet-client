using System.CommandLine;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Resolver.Athena.Client.HighLevelClient.DependencyInjection;
using Resolver.Athena.Client.HighLevelClient.Images;
using Resolver.Athena.Client.HighLevelClient.Interfaces;
using Resolver.Athena.Client.HighLevelClient.Models;

namespace SimpleClient;

public static class ClassifyCommand
{
    public static void RegisterCommand(RootCommand rootCommand)
    {
        var cmd = rootCommand.AddAthenaCommand("classify", "Classify images using the standard IAsyncEnumerable client", DoClassifyCommand);
        cmd.Options.Add(CliUtilities.RepeatOption);
        cmd.Arguments.Add(CliUtilities.DeploymentIdArgument);
        cmd.Arguments.Add(CliUtilities.ImagePathArgument);
    }

    public static async Task<int> DoClassifyCommand(ParseResult parseResult, CancellationToken cancellationToken)
    {
        CliUtilities.LoadDotEnv(parseResult);

        var svcs = new ServiceCollection()
            .AddAthenaClient(CliUtilities.ConfigureAthenaClientFromEnv, CliUtilities.ConfigureOAuthTokenManagerFromEnv)
            .BuildServiceProvider();

        var athenaClient = svcs.GetRequiredService<IAthenaClient>();
        var deploymentId = CliUtilities.GetDeploymentId(parseResult);
        var path = parseResult.GetValue(CliUtilities.ImagePathArgument) ?? throw new InvalidOperationException("Image path argument is required.");

        var repeatSeconds = parseResult.GetValue(CliUtilities.RepeatOption);

        try
        {
            // gather files: if a directory is provided, enumerate common image extensions; otherwise treat as single file
            var files = CliUtilities.GatherImagePaths(path);

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
                        var sanitized = CliUtilities.SanitizeCorrelationId(rawCorrelation);

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
            Console.WriteLine(CliUtilities.GenerateStreamSummary(requestsToSend, consumedResponses, errorResponses));

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to classify images (stream): {ex.Message}");
            return 1;
        }
    }
}
