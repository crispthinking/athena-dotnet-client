using Resolver.Athena.Grpc;

namespace Resolver.Athena.Models;

public class ClassificationResult
{
    public string CorrelationId { get; set; } = string.Empty;
    public List<ImageClassification> Classifications { get; set; } = [];

    public ClassificationErrorDetails? ErrorDetails { get; set; }

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
