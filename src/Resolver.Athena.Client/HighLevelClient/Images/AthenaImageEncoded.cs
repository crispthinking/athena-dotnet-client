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
    /// <exception cref="FormatException">Thrown when the image data cannot be decoded.</exception>
    public AthenaImageEncoded(byte[] data)
    {
        Mat image;
        try
        {
            image = Cv2.ImDecode(data, ImreadModes.Color);
        }
        catch (Exception ex)
        {
            throw new FormatException("Image data could not be decoded.", ex);
        }

        using (image)
        {
            if (image is null || image.Empty())
                throw new FormatException("Image data could not be decoded.");

            var needsResize = image.Width != AthenaConstants.ExpectedImageWidth ||
                              image.Height != AthenaConstants.ExpectedImageHeight;

            using var resized = needsResize ? new Mat() : null;
            if (needsResize)
                Cv2.Resize(image, resized!, new Size(AthenaConstants.ExpectedImageWidth, AthenaConstants.ExpectedImageHeight),
                           interpolation: InterpolationFlags.Linear);
            var source = resized ?? image;

            if (source.Type() != MatType.CV_8UC3)
                throw new ArgumentException(
                    $"Decoded image has unexpected type {source.Type()} (expected CV_8UC3).", nameof(data));

            _format = ImageFormat.RawUint8Bgr;

            var expectedByteLength = AthenaConstants.ExpectedImageWidth *
                                     AthenaConstants.ExpectedImageHeight *
                                     AthenaConstants.ExpectedImageChannels;

            var matByteLength = (int)(source.Total() * source.ElemSize());
            if (matByteLength != expectedByteLength)
                throw new ArgumentException(
                    $"Mat buffer size ({matByteLength}) does not match expected byte length ({expectedByteLength}).",
                    nameof(data));

            _bytes = new byte[expectedByteLength];

            // OpenCV loads images in BGR order by default — copy directly.
            // Ensure the source is contiguous so the linear copy has no row padding.
            using var clonedForContiguity = source.IsContinuous() ? null : source.Clone();
            Marshal.Copy((clonedForContiguity ?? source).Data, _bytes, 0, expectedByteLength);
        }
    }

    /// <inheritdoc />
    public override ImageFormat Format => _format;

    /// <inheritdoc />
    public override Span<byte> GetBytes() => _bytes;
}
