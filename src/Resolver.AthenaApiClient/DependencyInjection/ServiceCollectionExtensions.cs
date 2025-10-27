using Microsoft.Extensions.DependencyInjection;
using Resolver.AthenaApiClient.Auth;
using Resolver.AthenaApiClient.Clients;
using Resolver.AthenaApiClient.Interfaces;

namespace Resolver.AthenaApiClient.DependencyInjection;

/// <summary>
/// Registration helpers for the low-level Athena API client.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the OAuth token manager and related plumbing.
    /// </summary>
    public static IServiceCollection AddOAuthTokenManager(
        this IServiceCollection services,
        Action<OAuthTokenManagerConfiguration> configureTokenManager)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureTokenManager);

        services.AddHttpClient();
        services.AddLogging();
        services.Configure(configureTokenManager);
        services.AddSingleton<ITokenManager, OAuthTokenManager>();

        return services;
    }

    /// <summary>
    /// Registers the low-level Athena API client and supporting services.
    /// </summary>
    public static IServiceCollection AddAthenaApiClient(
        this IServiceCollection services,
        Action<AthenaApiClientConfiguration> configureClient,
        Action<OAuthTokenManagerConfiguration> configureTokenManager)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureClient);
        ArgumentNullException.ThrowIfNull(configureTokenManager);

        services.AddOAuthTokenManager(configureTokenManager);

        services.AddSingleton<IAthenaClassifierServiceClientFactory, AthenaClassifierServiceClientFactory>();
        services.AddSingleton<IAthenaApiClient, Clients.AthenaApiClient>();
        services.Configure(configureClient);

        return services;
    }
}
