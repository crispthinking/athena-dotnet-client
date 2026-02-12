using System.Runtime.InteropServices;
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
        using var image = Cv2.ImDecode(data, ImreadModes.Color);
        if (image is null || image.Empty())
            throw new ArgumentException("Image data could not be decoded.", nameof(data));

        var needsResize = image.Width != AthenaConstants.ExpectedImageWidth ||
                          image.Height != AthenaConstants.ExpectedImageHeight;

        using var resized = needsResize
            ? image.Resize(new Size(AthenaConstants.ExpectedImageWidth, AthenaConstants.ExpectedImageHeight),
                           interpolation: InterpolationFlags.Linear)
            : null;
        var source = resized ?? image;

        _format = ImageFormat.RawUint8Bgr;

        var totalPixels = AthenaConstants.ExpectedImageWidth *
                          AthenaConstants.ExpectedImageHeight *
                          AthenaConstants.ExpectedImageChannels;
        _bytes = new byte[totalPixels];

        // OpenCV loads images in BGR order by default — copy directly.
        // Ensure the source is contiguous so the linear copy has no row padding.
        using var contiguous = source.IsContinuous() ? null : source.Clone();
        Marshal.Copy((contiguous ?? source).Data, _bytes, 0, totalPixels);
    }

    /// <inheritdoc />
    public override ImageFormat Format => _format;

    /// <inheritdoc />
    public override Span<byte> GetBytes() => _bytes;
}
