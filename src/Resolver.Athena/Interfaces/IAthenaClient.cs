using Resolver.Athena.Models;

namespace Resolver.Athena.Interfaces;

public interface IAthenaClient
{
    public Task<List<DeploymentSummary>> ListDeploymentsAsync(CancellationToken cancellationToken);
}
