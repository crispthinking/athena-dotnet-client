using OpenCvSharp;
using Resolver.Athena.Client.ApiClient;
using Resolver.Athena.Grpc;

namespace Resolver.Athena.Client.HighLevelClient.Images;

/// <summary>
/// Represents an image that is encoded in a specific format (e.g., JPEG, PNG).
/// Uses OpenCvSharp with bilinear interpolation for resizing to match the
/// reference OpenCV preprocessing pipeline.
/// </summary>
public class AthenaImageEncoded : AthenaImageBase
{
    private readonly ImageFormat _format;
    private readonly byte[] _bytes;

    /// <summary>
    /// Creates a new instance of <see cref="AthenaImageEncoded"/> from the provided byte array.
    /// Decodes the image and resizes to the expected dimensions using bilinear interpolation.
    /// </summary>
    /// <param name="data">The byte array representing the encoded image data.</param>
    /// <exception cref="ArgumentException">Thrown when the image data cannot be decoded.</exception>
    public AthenaImageEncoded(byte[] data)
    {
        using var image = Cv2.ImDecode(data, ImreadModes.Color)
            ?? throw new ArgumentException("Image data could not be decoded.");

        if (image.Empty())
            throw new ArgumentException("Image data could not be decoded.");

        using var resized = image.Width == AthenaConstants.ExpectedImageWidth &&
                            image.Height == AthenaConstants.ExpectedImageHeight
            ? image.Clone()
            : image.Resize(new Size(AthenaConstants.ExpectedImageWidth, AthenaConstants.ExpectedImageHeight),
                           interpolation: InterpolationFlags.Linear);

        _format = ImageFormat.RawUint8Bgr;

        var totalPixels = AthenaConstants.ExpectedImageWidth *
                          AthenaConstants.ExpectedImageHeight *
                          AthenaConstants.ExpectedImageChannels;
        _bytes = new byte[totalPixels];

        // OpenCV loads images in BGR order by default — copy directly.
        // The Mat is contiguous HWC BGR uint8, which is exactly the format we need.
        unsafe
        {
            var src = (byte*)resized.DataPointer;
            for (var i = 0; i < totalPixels; i++)
            {
                _bytes[i] = src[i];
            }
        }
    }

    /// <inheritdoc />
    public override ImageFormat Format => _format;

    /// <inheritdoc />
    public override Span<byte> GetBytes() => _bytes;
}
