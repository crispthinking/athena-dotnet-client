using Resolver.Athena.Grpc;
using Resolver.AthenaClient.Images;

namespace Resolver.AthenaClient.Interfaces;

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
