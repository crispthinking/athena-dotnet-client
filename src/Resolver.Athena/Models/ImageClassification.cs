namespace Resolver.Athena.Models;

/// <summary>
/// Represents the result of an image classification.
/// </summary>
/// <param name="Label">The label assigned to the classified image.</param>
/// <param name="Confidence">The confidence score of the classification.</param>
public record ImageClassification(string Label, float Confidence);
