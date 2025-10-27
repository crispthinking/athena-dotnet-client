namespace Resolver.AthenaClient.Models;

/// <summary>
/// Represents the result of an image classification.
/// </summary>
public sealed record ImageClassification(string Label, float Confidence);
