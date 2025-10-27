using Resolver.Athena.Grpc;

namespace Resolver.AthenaApiClient.Interfaces;

/// <summary>
/// Represents an active bidirectional streaming session with the Athena classifier service.
/// </summary>
public interface IAthenaSession : IAsyncDisposable
{
    /// <summary>
    /// Sends a <see cref="ClassifyRequest"/> to the classifier stream.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">A cancellation token to observe while sending.</param>
    /// <returns>A task that completes when the request has been transmitted.</returns>
    ValueTask SendAsync(ClassifyRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Enumerates the responses produced by the classifier stream.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token used to cancel the enumeration.</param>
    /// <returns>An asynchronous sequence of <see cref="ClassifyResponse"/> instances.</returns>
    IAsyncEnumerable<ClassifyResponse> ReadResponsesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Signals that no additional requests will be sent on the stream.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while completing the request stream.</param>
    /// <returns>A task that completes once the underlying request stream has been closed.</returns>
    Task CompleteAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets a task that completes when the streaming call has concluded.
    /// </summary>
    Task ResponseCompletion { get; }
}
