using Resolver.Athena.Client.HighLevelClient.Images;

namespace Resolver.Athena.Client.HighLevelClient.Models;

/// <summary>
/// Describes a single image that should be classified.
/// </summary>
public sealed record ClassificationRequest(string DeploymentId, AthenaImageBase Image, string? CorrelationId = null);
