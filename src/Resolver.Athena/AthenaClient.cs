using Grpc.Core;
using Resolver.Athena.Grpc;
using Resolver.Athena.Interfaces;
using Resolver.Athena.Models;

namespace Resolver.Athena;

public class AthenaClient(ChannelBase channel) : IAthenaClient
{
    private readonly ClassifierService.ClassifierServiceClient _client = new(channel);
    private readonly CallOptions _callOptions = new();

    public async Task<List<DeploymentSummary>> ListDeploymentsAsync(CancellationToken cancellationToken)
    {
        var response = await _client.ListDeploymentsAsync(new(), _callOptions.WithCancellationToken(cancellationToken));
        return [.. response.Deployments.Select(deployment => new DeploymentSummary(deployment))];
    }
}
