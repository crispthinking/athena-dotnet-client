using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Resolver.Athena.Client.HighLevelClient.DependencyInjection;
using Resolver.Athena.Client.HighLevelClient.Images;
using Resolver.Athena.Client.HighLevelClient.Interfaces;

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

            Console.WriteLine(result.ToPrettyString());

            return result.ErrorDetails == null ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to classify image: {ex.Message}");
            return 1;
        }
    }

}
