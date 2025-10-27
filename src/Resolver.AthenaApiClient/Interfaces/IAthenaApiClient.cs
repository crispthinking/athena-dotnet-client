using System.Threading.Channels;
using Resolver.Athena.Grpc;

namespace Resolver.AthenaApiClient.Interfaces;

/// <summary>
/// Provides low-level access to the Athena classifier gRPC endpoints.
/// </summary>
public interface IAthenaApiClient
{
    /// <summary>
    /// Invokes the unary <c>ClassifySingle</c> RPC.
    /// </summary>
    /// <param name="input">The classification input payload.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The classifier output.</returns>
    Task<ClassificationOutput> ClassifySingleAsync(ClassificationInput input, CancellationToken cancellationToken);

    /// <summary>
    /// Invokes the unary <c>ListDeployments</c> RPC.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The raw deployment response payload.</returns>
    Task<ListDeploymentsResponse> ListDeploymentsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Invokes the bidirectional streaming <c>Classify</c> RPC.
    /// </summary>
    /// <param name="requestChannel">The channel providing classification requests.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The channel providing classification responses.</returns>
    Task<Channel<ClassifyResponse>> ClassifyAsync(ChannelReader<ClassifyRequest> requestChannel, int responseChannelCapacity, CancellationToken cancellationToken);
}
