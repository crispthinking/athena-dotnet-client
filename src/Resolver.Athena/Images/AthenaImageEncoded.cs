using Resolver.Athena.Extensions;
using Resolver.Athena.Grpc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Resolver.Athena.Images;

/// <summary>
/// Represents an image that is encoded in a specific format (e.g., JPEG, PNG).
///
/// This class handles image loading, resizing, and format conversion using the
/// ImageSharp library.
/// </summary>
public class AthenaImageEncoded : AthenaImageBase
{
    private readonly ImageFormat _format;
    private readonly byte[] _bytes;

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

    public override ImageFormat Format => _format;

    public override Span<byte> GetBytes()
    {
        return _bytes;
    }
}
