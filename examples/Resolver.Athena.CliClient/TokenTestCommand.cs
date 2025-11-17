using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Resolver.Athena.Client.ApiClient.DependencyInjection;
using Resolver.Athena.Client.ApiClient.Interfaces;

namespace Resolver.Athena.CliClient;

public static class TokenTestCommand
{
    public static void RegisterCommand(RootCommand rootCommand)
    {
        rootCommand.AddAthenaCommand("token-test", "Test OAuth token retrieval", DoTokenTestCommand);
    }

    public static async Task<int> DoTokenTestCommand(ParseResult parseResult, CancellationToken cancellationToken)
    {
        CliUtilities.LoadDotEnv(parseResult);
        var serviceProvider = new ServiceCollection()
            .AddOAuthTokenManager(CliUtilities.ConfigureOAuthTokenManagerFromEnv)
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

}
