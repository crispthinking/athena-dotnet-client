using Microsoft.Extensions.DependencyInjection;
using Resolver.Athena.Auth;
using Resolver.Athena.Interfaces;
using Resolver.Athena.LowLevel;

namespace Resolver.Athena.DependencyInjection;

public static class ServiceExtensions
{
    /// <summary>
    /// Adds the OAuthTokenManager and its dependencies to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="configure">An action to configure the OAuthTokenManagerConfiguration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddOAuthTokenManager(this IServiceCollection services, Action<OAuthTokenManagerConfiguration> configure)
    {
        return services
            .AddHttpClient()
            .AddLogging()
            .Configure(configure)
            .AddSingleton<ITokenManager, OAuthTokenManager>();
    }

    /// <summary>
    /// Adds the AthenaClient and its dependencies to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="athenaClientConfigure">An action to configure the AthenaClientConfiguration.</param>
    /// <param name="oAuthTokenManagerConfigure">An action to configure the OAuthTokenManagerConfiguration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAthenaClient(this IServiceCollection services, Action<AthenaClientConfiguration> athenaClientConfigure, Action<OAuthTokenManagerConfiguration> oAuthTokenManagerConfigure)
    {
        return services
            .AddOAuthTokenManager(oAuthTokenManagerConfigure)
            .AddSingleton<IAthenaClassifierServiceClientFactory, AthenaClassifierServiceClientFactory>()
            .Configure(athenaClientConfigure)
            .AddSingleton<IAthenaClient, AthenaClient>();
    }

    public static IServiceCollection AddPreBatchedLowLevelStreamingClient(this IServiceCollection services, Action<LowLevelStreamingClientConfiguration> lowLevelStreamingConfigure, Action<OAuthTokenManagerConfiguration> oAuthTokenManagerConfigure)
    {
        return services
            .AddOAuthTokenManager(oAuthTokenManagerConfigure)
            .AddSingleton<IAthenaClassifierServiceClientFactory, AthenaClassifierServiceClientFactory>()
            .Configure(lowLevelStreamingConfigure)
            .AddSingleton<IPreBatchedLowLevelStreamingClient, PreBatchedLowLevelStreamingClient>();
    }

    public static IServiceCollection AddBatchingLowLevelStreamingClient(this IServiceCollection services, Action<BatchingLowLevelStreamingClientConfiguration> lowLevelStreamingConfigure, Action<OAuthTokenManagerConfiguration> oAuthTokenManagerConfigure)
    {
        return services
            .AddOAuthTokenManager(oAuthTokenManagerConfigure)
            .AddSingleton<IAthenaClassifierServiceClientFactory, AthenaClassifierServiceClientFactory>()
            .Configure(lowLevelStreamingConfigure)
            .AddSingleton<IBatchingLowLevelStreamingClient, BatchingLowLevelStreamingClient>();
    }
}
