using System.CommandLine;
using dotenv.net;
using Microsoft.Extensions.DependencyInjection;
using Resolver.Athena;
using Resolver.Athena.Auth;
using Resolver.Athena.Interfaces;


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

    public static async Task Main(string[] args)
    {
        var svcProvider = new ServiceCollection()
            .AddHttpClient()
            .AddLogging()
            .Configure<OAuthTokenManagerConfiguration>(options =>
            {
                var newOpts = LoadOAuthOptions(".env");
                options.ClientId = newOpts.ClientId;
                options.ClientSecret = newOpts.ClientSecret;
                options.Audience = newOpts.Audience;
                options.AuthUrl = newOpts.AuthUrl;
            })
            .AddSingleton<ITokenManager, OAuthTokenManager>()
            .Configure<AthenaClientFactoryConfiguration>(options =>
            {
                var endpoint = Environment.GetEnvironmentVariable("ATHENA_ENDPOINT") ?? throw new InvalidOperationException("ATHENA_ENDPOINT not set in environment variables.");
                options.Endpoint = endpoint;
            })
            .AddSingleton<IAthenaClientFactory, AthenaClientFactory>()
            .BuildServiceProvider();

        var rootCommand = new RootCommand(Description);

        rootCommand.AddAthenaCommand("token-test", "Test OAuth token retrieval", svcProvider, DoTokenTestCommand);
        rootCommand.AddAthenaCommand("list-deployments", "List deployments from Athena", svcProvider, DoListDeploymentsCommand);

        var parseResult = rootCommand.Parse(args);
        await parseResult.InvokeAsync();
    }

    public static async Task<int> DoTokenTestCommand(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
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

    public static async Task<int> DoListDeploymentsCommand(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var athenaClient = serviceProvider.GetRequiredService<IAthenaClientFactory>().Create();

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

    private static OAuthTokenManagerConfiguration LoadOAuthOptions(string dotenvPath)
    {
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

        var clientId = Environment.GetEnvironmentVariable("OAUTH_CLIENT_ID") ?? throw new InvalidOperationException("OAUTH_CLIENT_ID not set in environment variables.");
        var clientSecret = Environment.GetEnvironmentVariable("OAUTH_CLIENT_SECRET") ?? throw new InvalidOperationException("OAUTH_CLIENT_SECRET not set in environment variables.");
        var audience = Environment.GetEnvironmentVariable("OAUTH_AUDIENCE");
        var authUrl = Environment.GetEnvironmentVariable("OAUTH_AUTH_URL");

        var options = new OAuthTokenManagerConfiguration
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
        };

        if (!string.IsNullOrEmpty(audience))
        {
            options.Audience = audience;
        }
        if (!string.IsNullOrEmpty(authUrl))
        {
            options.AuthUrl = authUrl;
        }

        return options;
    }

    public static void AddAthenaCommand(this Command parentCommand, string name, string description, IServiceProvider serviceProvider, Func<IServiceProvider, CancellationToken, Task<int>> commandHandler)
    {
        var command = new Command(name, description);
        command.SetAction((parseResult, cancellationToken) =>
        {
            return commandHandler(serviceProvider, cancellationToken);
        });
        parentCommand.Subcommands.Add(command);
    }
}
