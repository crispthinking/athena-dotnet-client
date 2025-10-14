using Resolver.Athena.Images;
using Resolver.Athena.Models;

namespace Resolver.Athena.Interfaces;

public interface IAthenaClient
{
    public Task<List<DeploymentSummary>> ListDeploymentsAsync(CancellationToken cancellationToken);

    public Task<ClassificationResult> ClassifySingleImageAsync(AthenaImageBase imageData, CancellationToken cancellationToken);
}
