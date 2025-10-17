using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Resolver.Athena.Grpc;
using Resolver.Athena.Images;
using Resolver.Athena.Interfaces;

namespace Resolver.Athena.Streaming.LowLevel;

/// <summary>
/// A low-level streaming client that batches incoming images with configurable
/// batching settings before sending them to the gRPC stream.
/// </summary>
public class BatchingLowLevelStreamingClient : LowLevelStreamingClientBase, IBatchingLowLevelStreamingClient
{
    private readonly Channel<AthenaImageBase> _channel;
    private readonly int _batchMaxSize;

    private Task? _senderTask;

    public BatchingLowLevelStreamingClient(ITokenManager tokenManager, IOptions<BatchingLowLevelStreamingClientConfiguration> options, IAthenaClassifierServiceClientFactory athenaClassifierServiceClientFactory) : base(tokenManager, options, athenaClassifierServiceClientFactory)
    {
        _batchMaxSize = options.Value.MaxBatchSize;
        _channel = Channel.CreateBounded<AthenaImageBase>(new BoundedChannelOptions(options.Value.ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
        _senderTask = Task.Run(() => SenderLoop(cancellationToken), cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.Complete();
        if (_senderTask != null)
        {
            await _senderTask;
        }
        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Sends a single image to the channel to be batched and sent to the gRPC stream.
    /// </summary>
    public async Task SendAsync(AthenaImageBase imageData, CancellationToken cancellationToken)
    {
        await _channel.Writer.WriteAsync(imageData, cancellationToken);
    }

    /// <summary>
    /// Reads images from the channel, batches them, and sends them to the gRPC stream.
    /// </summary>
    private async Task SenderLoop(CancellationToken cancellationToken)
    {
        if (RequestStream == null)
        {
            throw new InvalidOperationException($"Stream has not been started. Call {nameof(StartAsync)} first.");
        }

        var request = new ClassifyRequest
        {
            DeploymentId = DeploymentId
        };

        // Asynchronously await for at least one item to be available in the channel
        while (await _channel.Reader.WaitToReadAsync(cancellationToken))
        {
            // Read until we reach the batch size or the channel is empty
            while (request.Inputs.Count < _batchMaxSize && _channel.Reader.TryRead(out var imageData))
            {
                request.Inputs.Add(PrepareInput(imageData));
            }

            // Send the batch & reset it if we have any items
            if (request.Inputs.Count > 0)
            {
                await SendAsync(request, cancellationToken);
                request.Inputs.Clear();
            }
        }
    }
}
