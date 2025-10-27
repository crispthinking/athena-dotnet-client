using Resolver.Athena.Grpc;
using Resolver.AthenaApiClient;

namespace Resolver.AthenaClient.Images;

/// <summary>
/// Represents an image in raw unsigned 8-bit format.
/// </summary>
public class AthenaImageRawUInt8 : AthenaImageBase
{
    private readonly byte[] _data;

    /// <summary>
    /// Creates a new instance of <see cref="AthenaImageRawUInt8"/> from the provided byte array.
    /// </summary>
    /// <param name="data">The byte array representing the image data.</param>
    /// <exception cref="ArgumentException">Thrown when the data length is invalid.</exception>
    public AthenaImageRawUInt8(byte[] data)
    {
        var expectedBytes = AthenaConstants.ExpectedImageWidth *
                            AthenaConstants.ExpectedImageHeight *
                            AthenaConstants.ExpectedImageChannels;

        if (data.Length != expectedBytes)
        {
            throw new ArgumentException($"Data length must be {expectedBytes} bytes for a {AthenaConstants.ExpectedImageWidth}x{AthenaConstants.ExpectedImageHeight} image with {AthenaConstants.ExpectedImageChannels} channels.");
        }

        _data = data;
    }

    /// <inheritdoc />
    public override ImageFormat Format => ImageFormat.RawUint8;

    /// <inheritdoc />
    public override Span<byte> GetBytes() => _data;
}
