using Resolver.Athena.Grpc;

namespace Resolver.AthenaClient.Models;

/// <summary>
/// Represents an Athena deployment with its ID and backlog count.
/// </summary>
public sealed record DeploymentSummary(string DeploymentId, int Backlog)
{
    /// <summary>
    /// Initializes a new instance from a gRPC deployment payload.
    /// </summary>
    public DeploymentSummary(Deployment deployment) : this(deployment.DeploymentId, deployment.Backlog)
    {
    }
}
