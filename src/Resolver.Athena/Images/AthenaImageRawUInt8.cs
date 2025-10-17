using Resolver.Athena.Grpc;

namespace Resolver.Athena.Images;

/// <summary>
/// Represents an image in raw unsigned 8-bit format (RawUInt8). The image is
/// expected to have a fixed width, height, and number of channels as defined
/// in AthenaConstants.
/// </summary>
public class AthenaImageRawUInt8 : AthenaImageBase
{
    private readonly byte[] _data;
    private readonly string _correlationId;

    public AthenaImageRawUInt8(byte[] data, string? correlationId = null)
    {
        var expectedBytes = AthenaConstants.ExpectedImageWidth *
                            AthenaConstants.ExpectedImageHeight *
                            AthenaConstants.ExpectedImageChannels;

        if (data.Length != expectedBytes)
        {
            throw new ArgumentException($"Data length must be {expectedBytes} bytes for a {AthenaConstants.ExpectedImageWidth}x{AthenaConstants.ExpectedImageHeight} image with {AthenaConstants.ExpectedImageChannels} channels.");
        }

        _data = data;
        _correlationId = correlationId ?? Guid.NewGuid().ToString();
    }

    public override string CorrelationId => _correlationId;

    public override ImageFormat Format => ImageFormat.RawUint8;

    public override Span<byte> GetBytes()
    {
        return _data;
    }
}
