using Resolver.Athena.Grpc;

namespace Resolver.Athena.Models;

/// <summary>
/// Result of an image classification operation.
/// </summary>
public class ClassificationResult
{
    /// <summary>
    /// The correlation ID associated with the classification request.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// The list of image classifications.
    /// </summary>
    public List<ImageClassification> Classifications { get; set; } = [];

    /// <summary>
    /// Details of any error that occurred during classification.
    /// </summary>
    public ClassificationErrorDetails? ErrorDetails { get; set; }

    /// <summary>
    /// Creates a <see cref="ClassificationResult"/>  from a single <see cref="ClassificationOutput"/>.
    /// </summary>
    /// <param name="output">The <see cref="ClassificationOutput"/> to convert.</param>
    /// <returns>A <see cref="ClassificationResult"/> instance.</returns>
    public static ClassificationResult FromSingleOutput(ClassificationOutput output)
    {
        var result = new ClassificationResult
        {
            CorrelationId = output.CorrelationId
        };

        foreach (var classification in output.Classifications)
        {
            result.Classifications.Add(new ImageClassification(classification.Label, classification.Weight));
        }

        if (output.Error != null)
        {
            result.ErrorDetails = new ClassificationErrorDetails
            {
                Code = ClassificationErrorCodeExtensions.FromGrpc(output.Error.Code),
                Message = output.Error.Message,
                AdditionalDetails = output.Error.Details
            };
        }

        return result;
    }
}
