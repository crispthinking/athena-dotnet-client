using Resolver.Athena.Client.ApiClient;
using Resolver.Athena.Grpc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Resolver.Athena.Client.HighLevelClient.Images;

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

        if (image.Width != AthenaConstants.ExpectedImageWidth ||
            image.Height != AthenaConstants.ExpectedImageHeight)
        {
            image.Mutate(x => x.Resize(AthenaConstants.ExpectedImageWidth, AthenaConstants.ExpectedImageHeight));
        }

        _format = ImageFormat.RawUint8Bgr;

        _bytes = new byte[AthenaConstants.ExpectedImageWidth *
                          AthenaConstants.ExpectedImageHeight *
                          AthenaConstants.ExpectedImageChannels];

        using var rgbImage = image switch
        {
            Image<Rgb24> rgb => rgb,
            _ => image.CloneAs<Rgb24>()
        };

        for (var y = 0; y < AthenaConstants.ExpectedImageHeight; y++)
        {
            for (var x = 0; x < AthenaConstants.ExpectedImageWidth; x++)
            {
                var pixel = rgbImage[x, y];
                var idx = (y * AthenaConstants.ExpectedImageWidth + x) * AthenaConstants.ExpectedImageChannels;
                _bytes[idx] = pixel.B;
                _bytes[idx + 1] = pixel.G;
                _bytes[idx + 2] = pixel.R;
            }
        }
    }

    /// <inheritdoc />
    public override ImageFormat Format => _format;

    /// <inheritdoc />
    public override Span<byte> GetBytes() => _bytes;
}
