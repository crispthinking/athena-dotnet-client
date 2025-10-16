using Microsoft.Extensions.Options;
using Resolver.Athena.Grpc;
using Resolver.Athena.Interfaces;

namespace Resolver.Athena.LowLevel;

/// <summary>
/// A low-level streaming client that sends pre-batched requests directly to
/// the gRPC stream, without doing any internal batching.
/// </summary>
public class PreBatchedLowLevelStreamingClient(ITokenManager tokenManager, IOptions<LowLevelStreamingConfiguration> options, IAthenaClassifierServiceClientFactory athenaClassifierServiceClientFactory) : LowLevelStreamingClientBase(tokenManager, options, athenaClassifierServiceClientFactory), IPreBatchedLowLevelStreamingClient
{
    /// <summary>
    /// Sends a pre-batched request directly to the gRPC stream.
    /// </summary>
    public async Task SendBatchAsync(ClassifyRequest request, CancellationToken cancellationToken)
    {
        if (request.Inputs.Count == 0)
        {
            throw new ArgumentException("Request must contain at least one image.", nameof(request));
        }

        if (request.DeploymentId is null)
        {
            request.DeploymentId = DeploymentId;
        }
        else if (request.DeploymentId != DeploymentId)
        {
            throw new ArgumentException($"Request DeploymentId '{request.DeploymentId}' does not match client DeploymentId '{DeploymentId}'.", nameof(request));
        }

        await SendAsync(request, cancellationToken);
    }
}
