using Microsoft.Extensions.DependencyInjection;
using Resolver.Athena.Client.ApiClient;
using Resolver.Athena.Client.ApiClient.Auth;
using Resolver.Athena.Client.HighLevelClient.DependencyInjection;
using Resolver.Athena.Client.HighLevelClient.Options;
using Resolver.Athena.Client.TPL.Interfaces;
using Resolver.Athena.Client.TPL.Options;

namespace Resolver.Athena.Client.TPL.DependencyInjection;

/// <summary>
/// Registration helpers for the dataflow client.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the dataflow client and dependencies.
    /// </summary>
    public static IServiceCollection AddAthenaDataflowClient(
        this IServiceCollection services,
        Action<AthenaApiClientConfiguration> configureClient,
        Action<OAuthTokenManagerConfiguration> configureTokenManager,
        Action<AthenaClientOptions>? configureStreaming = null,
        Action<AthenaDataflowClientOptions>? configureDataflow = null)
    {
        services.AddAthenaClient(configureClient, configureTokenManager, configureStreaming);

        services.AddOptions<AthenaDataflowClientOptions>();
        services.AddSingleton<IAthenaDataflowClient, AthenaDataflowClient>();

        if (configureDataflow is not null)
        {
            services.Configure(configureDataflow);
        }

        return services;
    }
}
