using Resolver.Athena.Grpc;
using Resolver.Athena.Interfaces;
using Resolver.Athena.Models;

namespace Resolver.Athena;

public class AthenaClient(ClassifierService.ClassifierServiceClient client) : IAthenaClient
{
    private readonly ClassifierService.ClassifierServiceClient _client = client;

    public async Task<List<DeploymentSummary>> ListDeploymentsAsync(CancellationToken cancellationToken)
    {
        var response = await _client.ListDeploymentsAsync(new(), cancellationToken: cancellationToken);
        return [.. response.Deployments.Select(deployment => new DeploymentSummary(deployment))];
    }
}
