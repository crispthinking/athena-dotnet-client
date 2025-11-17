using System.CommandLine;

namespace Resolver.Athena.CliClient;

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

    public static async Task Main(string[] args)
    {
        var rootCommand = new RootCommand(Description);

        // reuse the pre-defined static option so LoadDotEnv can access the same option
        rootCommand.Options.Add(CliUtilities.DotenvPathOption);

        TokenTestCommand.RegisterCommand(rootCommand);
        ListDeploymentsCommand.RegisterCommand(rootCommand);
        ClassifySingleCommand.RegisterCommand(rootCommand);
        ClassifyCommand.RegisterCommand(rootCommand);
        ClassifyDataflowCommand.RegisterCommand(rootCommand);

        var parseResult = rootCommand.Parse(args);
        await parseResult.InvokeAsync();
    }
}
