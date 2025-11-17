namespace Resolver.Athena.Client.HighLevelClient.Models;

/// <summary>
/// Represents the result of an image classification.
/// </summary>
public sealed record ImageClassification(string Label, float Confidence);
