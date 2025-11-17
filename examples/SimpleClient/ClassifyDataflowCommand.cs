using System.CommandLine;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.DependencyInjection;
using Resolver.Athena.Client.HighLevelClient.Images;
using Resolver.Athena.Client.HighLevelClient.Models;
using Resolver.Athena.Client.TPL.DependencyInjection;
using Resolver.Athena.Client.TPL.Interfaces;

namespace SimpleClient;

public static class ClassifyDataflowCommand
{
    public static void RegisterCommand(RootCommand rootCommand)
    {
        var cmd = rootCommand.AddAthenaCommand("classify-dataflow", "Classify images using the TPL Dataflow Client", DoClassifyDataflowCommand);
        cmd.Options.Add(CliUtilities.RepeatOption);
        cmd.Arguments.Add(CliUtilities.DeploymentIdArgument);
        cmd.Arguments.Add(CliUtilities.ImagePathArgument);
    }

    public static async Task<int> DoClassifyDataflowCommand(ParseResult parseResult, CancellationToken cancellationToken)
    {
        CliUtilities.LoadDotEnv(parseResult);

        var svcs = new ServiceCollection()
            .AddAthenaDataflowClient(CliUtilities.ConfigureAthenaClientFromEnv, CliUtilities.ConfigureOAuthTokenManagerFromEnv)
            .BuildServiceProvider();

        var athenaClient = svcs.GetRequiredService<IAthenaDataflowClient>();
        var pipeline = await athenaClient.CreatePipelineAsync(cancellationToken);
        var deploymentId = CliUtilities.GetDeploymentId(parseResult);
        var path = parseResult.GetValue(CliUtilities.ImagePathArgument) ?? throw new InvalidOperationException("Image path argument is required.");

        var repeatSeconds = parseResult.GetValue(CliUtilities.RepeatOption);

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
                var files = CliUtilities.GatherImagePaths(path);
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
                var sanitized = CliUtilities.SanitizeCorrelationId(rawCorrelation);

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

                Console.WriteLine(result.ToPrettyString());

                if (result.ErrorDetails != null)
                {
                    errorResponses++;
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

            Console.WriteLine(CliUtilities.GenerateStreamSummary(await requestsToSendCount.Task, consumedResponses, errorResponses));

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to classify images (stream): {ex.Message}");
            return 1;
        }
    }

}
