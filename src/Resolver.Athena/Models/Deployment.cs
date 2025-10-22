using Resolver.Athena.Grpc;

namespace Resolver.Athena.Models;

/// <summary>
/// Represents an Athena deployment with its ID and backlog count.
/// </summary>
public record class DeploymentSummary(string DeploymentId, int Backlog)
{
    /// <summary>
    /// Initializes a new instance of <see cref="DeploymentSummary"/> from a Deployment object.
    /// </summary>
    /// <param name="deployment">The <see cref="Deployment"/> object.</param>
    public DeploymentSummary(Deployment deployment) : this(deployment.DeploymentId, deployment.Backlog) { }
}
