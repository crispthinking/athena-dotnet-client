using Resolver.Athena.Grpc;
using Resolver.AthenaApiClient;
using Resolver.AthenaClient.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Resolver.AthenaClient.Images;

/// <summary>
/// Represents an image that is encoded in a specific format (e.g., JPEG, PNG).
/// </summary>
public class AthenaImageEncoded : AthenaImageBase
{
    private readonly ImageFormat _format;
    private readonly byte[] _bytes;

    /// <summary>
    /// Creates a new instance of <see cref="AthenaImageEncoded"/> from the provided byte array.
    /// </summary>
    /// <param name="data">The byte array representing the image data.</param>
    /// <exception cref="InvalidOperationException">Thrown when the image format cannot be determined.</exception>
    public AthenaImageEncoded(byte[] data)
    {
        using var image = Image.Load(data);
        var imageFormat = image?.Metadata?.DecodedImageFormat ?? throw new InvalidOperationException("Unable to determine image format.");

        if (image.Width != AthenaConstants.ExpectedImageWidth ||
            image.Height != AthenaConstants.ExpectedImageHeight)
        {
            image.Mutate(x => x.Resize(AthenaConstants.ExpectedImageWidth, AthenaConstants.ExpectedImageHeight));
        }

        _format = image.ToAthenaImageFormat();
        var memStream = new MemoryStream();
        image.Save(memStream, imageFormat);
        _bytes = memStream.ToArray();
    }

    /// <inheritdoc />
    public override ImageFormat Format => _format;

    /// <inheritdoc />
    public override Span<byte> GetBytes() => _bytes;
}
