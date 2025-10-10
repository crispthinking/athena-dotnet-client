using Resolver.Athena.Grpc;

namespace Resolver.Athena.Models;

/// <summary>
/// Represents an Athena deployment with its ID and backlog count.
/// </summary>
public record class DeploymentSummary(string DeploymentId, int Backlog)
{
    public DeploymentSummary(Deployment deployment) : this(deployment.DeploymentId, deployment.Backlog) { }
}
