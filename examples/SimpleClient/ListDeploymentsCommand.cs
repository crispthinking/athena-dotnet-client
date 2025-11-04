using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Resolver.AthenaClient.DependencyInjection;
using Resolver.AthenaClient.Interfaces;

namespace SimpleClient;

public static class ListDeploymentsCommand
{
    public static void RegisterCommand(RootCommand rootCommand)
    {
        rootCommand.AddAthenaCommand("list-deployments", "List all deployments", DoListDeploymentsCommand);
    }

    public static async Task<int> DoListDeploymentsCommand(ParseResult parseResult, CancellationToken cancellationToken)
    {
        CliUtilities.LoadDotEnv(parseResult);

        var svcs = new ServiceCollection()
            .AddAthenaClient(CliUtilities.ConfigureAthenaClientFromEnv, CliUtilities.ConfigureOAuthTokenManagerFromEnv)
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
}
