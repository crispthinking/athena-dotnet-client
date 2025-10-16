using Microsoft.Extensions.Options;
using Resolver.Athena.Grpc;
using Resolver.Athena.Images;
using Resolver.Athena.Interfaces;

namespace Resolver.Athena.LowLevel;

/// <summary>
/// A low-level streaming client that sends pre-batched requests directly to
/// the gRPC stream, without doing any internal batching.
/// </summary>
public class PreBatchedLowLevelStreamingClient(ITokenManager tokenManager, IOptions<LowLevelStreamingClientConfiguration> options, IAthenaClassifierServiceClientFactory athenaClassifierServiceClientFactory) : LowLevelStreamingClientBase(tokenManager, options, athenaClassifierServiceClientFactory), IPreBatchedLowLevelStreamingClient
{
    /// <summary>
    /// Sends a pre-batched request directly to the gRPC stream.
    /// </summary>
    public async Task SendBatchAsync(AthenaImageBase[] batch, CancellationToken cancellationToken)
    {
        if (batch.Length == 0)
        {
            throw new ArgumentException("Batch must contain at least one image.", nameof(batch));
        }

        var request = new ClassifyRequest
        {
            DeploymentId = DeploymentId
        };

        request.Inputs.AddRange(batch.Select(PrepareInput));

        await SendAsync(request, cancellationToken);
    }
}
