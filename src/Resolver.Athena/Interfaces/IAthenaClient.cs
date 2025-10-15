using Resolver.Athena.Images;
using Resolver.Athena.Models;

namespace Resolver.Athena.Interfaces;

public interface IAthenaClient
{
    /// <summary>
    /// Lists all deployments for the current affiliate and their backlogs.
    ///
    /// Backlogs are indicative and may not be real-time.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of deployment summaries.</returns>
    public Task<List<DeploymentSummary>> ListDeploymentsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Classifies a single image and returns the classification results.
    /// </summary>
    /// <param name="imageData">The image data to classify.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the classification results.</returns>
    public Task<ClassificationResult> ClassifySingleImageAsync(AthenaImageBase imageData, CancellationToken cancellationToken);
}
