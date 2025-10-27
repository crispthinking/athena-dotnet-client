using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Grpc.Core;
using Resolver.Athena.Grpc;
using Resolver.AthenaApiClient.Interfaces;

namespace Resolver.AthenaApiClient.Clients;

/// <summary>
/// Default implementation of <see cref="IAthenaSession"/> that manages the lifecycle of an active gRPC duplex stream.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AthenaSession"/> class.
/// </remarks>
/// <param name="call">The underlying gRPC call.</param>
public sealed class AthenaSession : IAthenaSession
{
    private readonly AsyncDuplexStreamingCall<ClassifyRequest, ClassifyResponse> _call;
    private readonly TaskCompletionSource<bool> _responseCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly Channel<ClassifyRequest> _writeChannel;
    private readonly CancellationTokenSource _writerCts = new();
    private readonly Task _writerTask;

    private volatile bool _requestCompleted;
    private volatile bool _disposed;
    private int _responsesEnumerated;

    public AthenaSession(AsyncDuplexStreamingCall<ClassifyRequest, ClassifyResponse> call, int capacity = 1024)
    {
        _call = call ?? throw new ArgumentNullException(nameof(call));

        var options = new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        };

        _writeChannel = Channel.CreateBounded<ClassifyRequest>(options);
        _writerTask = Task.Run(() => WriterLoopAsync(_writerCts.Token));
    }

    /// <inheritdoc />
    public async ValueTask SendAsync(ClassifyRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ThrowIfDisposed();

        if (_requestCompleted)
        {
            throw new InvalidOperationException("The request stream has already been completed.");
        }

        await _writeChannel.Writer.WriteAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CompleteAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        if (_requestCompleted)
        {
            return;
        }

        _writeChannel.Writer.TryComplete();

        try
        {
            await _writerTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException();
        }

        _requestCompleted = true;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ClassifyResponse> ReadResponsesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        if (Interlocked.Exchange(ref _responsesEnumerated, 1) != 0)
        {
            throw new InvalidOperationException("The response stream has already been enumerated.");
        }

        while (true)
        {
            bool hasNext;
            try
            {
                hasNext = await _call.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _responseCompletion.TrySetException(ex);
                throw;
            }

            if (!hasNext)
            {
                _responseCompletion.TrySetResult(true);
                yield break;
            }

            yield return _call.ResponseStream.Current;
        }
    }

    /// <inheritdoc />
    public Task ResponseCompletion => _responseCompletion.Task;

    private async Task WriterLoopAsync(CancellationToken token)
    {
        try
        {
            await foreach (var req in _writeChannel.Reader.ReadAllAsync(token).ConfigureAwait(false))
            {
                await _call.RequestStream.WriteAsync(req, token).ConfigureAwait(false);
            }

            await _call.RequestStream.CompleteAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // best-effort attempt to complete gRPC stream
            try
            {
                await _call.RequestStream.CompleteAsync().ConfigureAwait(false);
            }
            catch
            {
            }
        }
        catch (Exception ex)
        {
            _responseCompletion.TrySetException(ex);
            _writeChannel.Writer.TryComplete(ex);
            throw;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(AthenaSession));
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _writerCts.Cancel();
            _writeChannel.Writer.TryComplete();
            await _writerTask.ConfigureAwait(false);
        }
        catch
        {
        }
        finally
        {
            try
            {
                _call.Dispose();
            }
            finally
            {
                _responseCompletion.TrySetResult(true);
            }

            _writerCts.Dispose();
        }
    }
}
