using Resolver.Athena.Client.HighLevelClient.Images;
using Resolver.Athena.Client.HighLevelClient.Models;

namespace Resolver.Athena.Client.HighLevelClient.Interfaces;

/// <summary>
/// Client interface for interacting with the Athena service.
/// </summary>
public interface IAthenaClient
{
    /// <summary>
    /// Lists all deployments for the current affiliate and their backlogs.
    /// </summary>
    Task<List<DeploymentSummary>> ListDeploymentsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Streams classifications for the provided requests.
    /// </summary>
    IAsyncEnumerable<ClassificationResult> ClassifyAsync(IAsyncEnumerable<ClassificationRequest> requests, CancellationToken cancellationToken);

    /// <summary>
    /// Classifies a single image and returns the classification results.
    /// </summary>
    Task<ClassificationResult> ClassifySingleAsync(AthenaImageBase imageData, CancellationToken cancellationToken);
}
