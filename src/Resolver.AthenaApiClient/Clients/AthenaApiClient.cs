using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using Resolver.Athena.Grpc;
using Resolver.AthenaApiClient.Interfaces;

namespace Resolver.AthenaApiClient.Clients;

/// <summary>
/// Default implementation of <see cref="IAthenaApiClient"/>.
/// </summary>
public sealed class AthenaApiClient : IAthenaApiClient
{
    private readonly ClassifierService.ClassifierServiceClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="AthenaApiClient"/> class.
    /// </summary>
    /// <param name="tokenManager">The token manager.</param>
    /// <param name="options">The client configuration options.</param>
    /// <param name="clientFactory">Factory used to create the underlying gRPC client.</param>
    public AthenaApiClient(ITokenManager tokenManager, IOptions<AthenaApiClientConfiguration> options, IAthenaClassifierServiceClientFactory clientFactory)
    {
        ArgumentNullException.ThrowIfNull(tokenManager);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clientFactory);

        var channelOptions = new GrpcChannelOptions
        {
            Credentials = ChannelCredentials.Create(
                new SslCredentials(),
                CallCredentials.FromInterceptor(async (context, metadata) =>
                {
                    var token = await tokenManager.GetTokenAsync(context.CancellationToken).ConfigureAwait(false);
                    metadata.Add("Authorization", $"Bearer {token}");
                }))
        };

        _client = clientFactory.Create(options.Value.Endpoint, channelOptions);
    }

    /// <inheritdoc />
    public Task<IAthenaSession> CreateSessionAsync(CancellationToken cancellationToken)
    {
        var call = _client.Classify(cancellationToken: cancellationToken);
        IAthenaSession session = new AthenaSession(call);
        return Task.FromResult(session);
    }

    /// <inheritdoc />
    public async Task<ClassificationOutput> ClassifySingleAsync(ClassificationInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        return await _client.ClassifySingleAsync(input, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ListDeploymentsResponse> ListDeploymentsAsync(CancellationToken cancellationToken)
    {
        return await _client.ListDeploymentsAsync(new Google.Protobuf.WellKnownTypes.Empty(), cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
