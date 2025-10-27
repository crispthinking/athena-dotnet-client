using System.Threading.Channels;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using Resolver.Athena.Grpc;
using Resolver.AthenaApiClient.Interfaces;

namespace Resolver.AthenaApiClient.Clients;

/// <summary>
/// Default implementation of <see cref="IAthenaApiClient"/>.
/// </summary>
public sealed class AthenaApiClient : IAthenaApiClient, IDisposable
{
    private readonly ClassifierService.ClassifierServiceClient _client;

    private Task? _senderTask = null;
    private Task? _receiverTask = null;

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

    /// <inheritdoc />
    public Task<Channel<ClassifyResponse>> ClassifyAsync(ChannelReader<ClassifyRequest> requestChannel, int responseChannelCapacity, CancellationToken cancellationToken)
    {
        if (_senderTask != null || _receiverTask != null)
        {
            throw new InvalidOperationException("ClassifyAsync can only be called once per AthenaApiClient instance.");
        }

        var call = _client.Classify(cancellationToken: cancellationToken);

        var responseChannel = Channel.CreateBounded<ClassifyResponse>(new BoundedChannelOptions(responseChannelCapacity)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        _senderTask = SenderLoopAsync(requestChannel, call.RequestStream, cancellationToken);
        _receiverTask = ReceiverLoopAsync(responseChannel, call.ResponseStream, cancellationToken);

        return Task.FromResult(responseChannel);
    }

    private static async Task SenderLoopAsync(ChannelReader<ClassifyRequest> requestChannel, IClientStreamWriter<ClassifyRequest> requestStream, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var req in requestChannel.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                await requestStream.WriteAsync(req, cancellationToken).ConfigureAwait(false);
            }

            await requestStream.CompleteAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // best-effort attempt to complete gRPC stream
            try
            {
                await requestStream.CompleteAsync().ConfigureAwait(false);
            }
            catch
            {
            }
        }
        catch (Exception)
        {
            await requestStream.CompleteAsync();
            throw;
        }
    }

    private static async Task ReceiverLoopAsync(Channel<ClassifyResponse> responseChannel, IAsyncStreamReader<ClassifyResponse> responseStream, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var resp in responseStream.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                await responseChannel.Writer.WriteAsync(resp, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            responseChannel.Writer.TryComplete(ex);
            throw;
        }
    }

    public void Dispose()
    {
        _senderTask?.Dispose();
        _receiverTask?.Dispose();
    }
}
