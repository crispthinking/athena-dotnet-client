using System.CommandLine;
using System.Text;
using System.Text.RegularExpressions;
using dotenv.net;
using Resolver.Athena.Client.ApiClient;
using Resolver.Athena.Client.ApiClient.Auth;

namespace SimpleClient;

public static partial class CliUtilities
{
    public static readonly Option<string> DotenvPathOption = new("--dotenv", "-d")
    {
        Description = "Path to .env file containing configuration",
        DefaultValueFactory = _ => ".env",
        Recursive = true,
    };

    public static readonly Argument<string> ImagePathArgument = new("image-path")
    {
        Description = "Path to the image file to classify",
    };

    public static readonly Argument<string> DeploymentIdArgument = new("deployment-id")
    {
        Description = "Deployment ID to target for streaming classify. Use 'random' to generate a random ID.",
    };

    public static readonly Option<int> RepeatOption = new("--repeat", "-r")
    {
        Description = "If set to >0, repeat sending images every N seconds until cancelled",
        DefaultValueFactory = _ => 0,
    };


    [GeneratedRegex("[^A-Za-z0-9_-]")]
    private static partial Regex CorrelationIdRegex();

    public static string SanitizeCorrelationId(string raw)
    {
        var sanitized = CorrelationIdRegex().Replace(raw, "_");
        return string.IsNullOrWhiteSpace(sanitized)
            ? Guid.NewGuid().ToString("N")
            : sanitized;
    }

    public static Action<OAuthTokenManagerConfiguration> ConfigureOAuthTokenManagerFromEnv => options =>
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

    public static Action<AthenaApiClientConfiguration> ConfigureAthenaClientFromEnv => options =>
    {
        var endpoint = Environment.GetEnvironmentVariable("ATHENA_ENDPOINT") ?? throw new InvalidOperationException("ATHENA_ENDPOINT not set in environment variables.");
        options.Endpoint = endpoint;
        var affiliate = Environment.GetEnvironmentVariable("ATHENA_AFFILIATE") ?? throw new InvalidOperationException("ATHENA_AFFILIATE not set in environment variables.");
        options.Affiliate = affiliate;
        options.SendMd5Hash = true;
        options.SendSha1Hash = true;
    };

    public static void LoadDotEnv(ParseResult parseResult)
    {
        var dotenvPath = parseResult.GetValue(DotenvPathOption) ?? ".env";
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

    public static string GetDeploymentId(ParseResult parseResult)
    {
        var deploymentId = parseResult.GetValue(DeploymentIdArgument) ?? throw new InvalidOperationException("deployment-id argument is required.");
        if (deploymentId.Equals("random", StringComparison.OrdinalIgnoreCase))
        {
            deploymentId = Guid.NewGuid().ToString("N");
        }
        return deploymentId;
    }

    public static List<string> GatherImagePaths(string path)
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

    public static string GenerateStreamSummary(int sentRequests, int receivedResponses, int errorResponses)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Stream Summary:");
        sb.AppendLine($"- Total Requests Sent: {sentRequests}");
        sb.AppendLine($"- Total Responses Received: {receivedResponses}");
        sb.AppendLine($"- Total Error Responses: {errorResponses}");
        string errorRate;
        if (receivedResponses > 0)
        {
            errorRate = $"{(double)errorResponses / receivedResponses * 100:F2}%";
        }
        else
        {
            errorRate = "N/A";
        }
        sb.AppendLine($"- Error Rate: {errorRate:F2}");

        return sb.ToString();
    }
}
