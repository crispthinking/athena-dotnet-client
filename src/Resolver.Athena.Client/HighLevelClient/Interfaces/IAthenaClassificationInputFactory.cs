using Resolver.Athena.Client.HighLevelClient.Images;
using Resolver.Athena.Grpc;

namespace Resolver.Athena.Client.HighLevelClient.Interfaces;

/// <summary>
/// Creates <see cref="ClassificationInput"/> payloads from Athena image abstractions.
/// </summary>
public interface IAthenaClassificationInputFactory
{
    /// <summary>
    /// Creates a <see cref="ClassificationInput"/> by copying data and metadata from the supplied image.
    /// </summary>
    ClassificationInput Create(AthenaImageBase image, string? correlationId = null);
}
