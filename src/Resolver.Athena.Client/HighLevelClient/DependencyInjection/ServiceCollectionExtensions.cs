using Microsoft.Extensions.DependencyInjection;
using Resolver.Athena.Client.ApiClient;
using Resolver.Athena.Client.ApiClient.Auth;
using Resolver.Athena.Client.ApiClient.DependencyInjection;
using Resolver.Athena.Client.HighLevelClient.Factories;
using Resolver.Athena.Client.HighLevelClient.Interfaces;
using Resolver.Athena.Client.HighLevelClient.Options;

namespace Resolver.Athena.Client.HighLevelClient.DependencyInjection;

/// <summary>
/// Registration helpers for the high-level Athena client.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the async enumerable client and dependencies.
    /// </summary>
    public static IServiceCollection AddAthenaClient(
        this IServiceCollection services,
        Action<AthenaApiClientConfiguration> configureClient,
        Action<OAuthTokenManagerConfiguration> configureTokenManager,
        Action<AthenaClientOptions>? configureStreaming = null)
    {
        services.AddAthenaApiClient(configureClient, configureTokenManager);

        services.AddSingleton<IAthenaClassificationInputFactory, AthenaClassificationInputFactory>();
        services.AddSingleton<IAthenaClient, AthenaClient>();
        services.AddOptions<AthenaClientOptions>();

        if (configureStreaming is not null)
        {
            services.Configure(configureStreaming);
        }

        return services;
    }
}
