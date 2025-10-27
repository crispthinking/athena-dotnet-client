using Resolver.AthenaClient.Images;

namespace Resolver.AthenaClient.Models;

/// <summary>
/// Describes a single image that should be classified.
/// </summary>
public sealed record ClassificationRequest(string DeploymentId, AthenaImageBase Image, string? CorrelationId = null);
