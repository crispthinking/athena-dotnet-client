using Resolver.Athena.Grpc;

namespace Resolver.AthenaApiClient.Interfaces;

/// <summary>
/// Provides low-level access to the Athena classifier gRPC endpoints.
/// </summary>
public interface IAthenaApiClient
{
    /// <summary>
    /// Creates a new bidirectional streaming session to the classifier service.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The active classifier session.</returns>
    Task<IAthenaSession> CreateSessionAsync(CancellationToken cancellationToken);

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
}
