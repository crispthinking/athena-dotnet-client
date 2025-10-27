using Microsoft.Extensions.DependencyInjection;
using Resolver.AthenaApiClient;
using Resolver.AthenaApiClient.Auth;
using Resolver.AthenaApiClient.DependencyInjection;
using Resolver.AthenaClient.Factories;
using Resolver.AthenaClient.Interfaces;
using Resolver.AthenaClient.Options;

namespace Resolver.AthenaClient.DependencyInjection;

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
