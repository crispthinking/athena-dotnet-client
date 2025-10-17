using Resolver.Athena.Grpc;
using Resolver.Athena.Images;
using Resolver.Athena.Streaming.LowLevel;

namespace Resolver.Athena.Streaming;

public class AthenaStreamingClientAsyncEnumerable
{
    private IAsyncEnumerable<AthenaImageBase>? _imageEnumerable;
    private CancellationTokenSource? _sendingCancellationTokenSource;
    private Task? _sendingTask;

    private readonly IBatchingLowLevelStreamingClient _lowLevelStreamingClient;

    public AthenaStreamingClientAsyncEnumerable(IBatchingLowLevelStreamingClient lowLevelStreamingClient)
    {
        _lowLevelStreamingClient = lowLevelStreamingClient;
    }

    public async Task StartAsync(IAsyncEnumerable<AthenaImageBase> imageAsyncEnumerable, CancellationToken cancellationToken)
    {
        _imageEnumerable = imageAsyncEnumerable;
        await _lowLevelStreamingClient.StartAsync(cancellationToken);

        _sendingCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _sendingTask = Task.Run(() => SendImagesAsync(_sendingCancellationTokenSource.Token), cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _lowLevelStreamingClient.StopAsync(cancellationToken);

        _sendingCancellationTokenSource?.Cancel();

        if (_sendingTask != null)
        {
            await _sendingTask;
        }
    }

    public IAsyncEnumerable<ClassifyResponse> GetResponsesAsync(CancellationToken cancellationToken)
    {
        return _lowLevelStreamingClient.GetResponsesAsync(cancellationToken);
    }

    protected async Task SendImagesAsync(CancellationToken cancellationToken)
    {
        if (_imageEnumerable == null)
        {
            throw new InvalidOperationException($"Image enumerator is not initialized. Call {nameof(StartAsync)} first.");
        }

        await foreach (var image in _imageEnumerable.WithCancellation(cancellationToken))
        {
            await _lowLevelStreamingClient.SendAsync(image, cancellationToken);
        }
    }
}
