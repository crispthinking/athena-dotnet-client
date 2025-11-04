using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Resolver.AthenaClient.DependencyInjection;
using Resolver.AthenaClient.Images;
using Resolver.AthenaClient.Interfaces;

namespace SimpleClient;

public static class ClassifySingleCommand
{
    public static void RegisterCommand(RootCommand rootCommand)
    {
        var cmd = rootCommand.AddAthenaCommand("classify-single", "Classify a single image using the Athena API.", DoClassifySingleCommand);
        cmd.Arguments.Add(CliUtilities.ImagePathArgument);
    }

    public static async Task<int> DoClassifySingleCommand(ParseResult parseResult, CancellationToken cancellationToken)
    {
        CliUtilities.LoadDotEnv(parseResult);
        var svcs = new ServiceCollection()
            .AddAthenaClient(CliUtilities.ConfigureAthenaClientFromEnv, CliUtilities.ConfigureOAuthTokenManagerFromEnv)
            .BuildServiceProvider();

        var athenaClient = svcs.GetRequiredService<IAthenaClient>();
        var imagePath = parseResult.GetValue(CliUtilities.ImagePathArgument) ?? throw new InvalidOperationException("Image path argument is required.");

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

}
