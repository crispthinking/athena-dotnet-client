using Microsoft.Extensions.DependencyInjection;
using Resolver.AthenaApiClient;
using Resolver.AthenaApiClient.Auth;
using Resolver.AthenaClient.DependencyInjection;
using Resolver.AthenaClient.Options;
using Resolver.AthenaDataflowClient.Interfaces;
using Resolver.AthenaDataflowClient.Options;
using AthenaDataflowClientImplementation = Resolver.AthenaDataflowClient.Clients.AthenaDataflowClient;

namespace Resolver.AthenaDataflowClient.DependencyInjection;

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
    services.AddSingleton<IAthenaDataflowClient, AthenaDataflowClientImplementation>();

        if (configureDataflow is not null)
        {
            services.Configure(configureDataflow);
        }

        return services;
    }
}
