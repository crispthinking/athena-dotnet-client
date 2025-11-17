using Resolver.Athena.Client.ApiClient;
using Resolver.Athena.Client.HighLevelClient.Images;
using Resolver.Athena.Grpc;

namespace Resolver.Athena.Tests.Images;

public class AthenaImageRawUInt8Tests
{
    [Fact]
    public void Constructor_ValidData_SetsFormatAndBytes()
    {
        // Arrange
        var expectedBytes = AthenaConstants.ExpectedImageWidth *
                            AthenaConstants.ExpectedImageHeight *
                            AthenaConstants.ExpectedImageChannels;
        var imageData = new byte[expectedBytes];
        new Random().NextBytes(imageData);

        // Act
        var athenaImage = new AthenaImageRawUInt8(imageData);

        // Assert
        Assert.Equal(ImageFormat.RawUint8, athenaImage.Format);
        var bytes = athenaImage.GetBytes();
        Assert.Equal(expectedBytes, bytes.Length);
        Assert.True(imageData.SequenceEqual(bytes.ToArray()));
    }

    [Fact]
    public void Constructor_InvalidDataLength_ThrowsException()
    {
        // Arrange
        var invalidImageData = new byte[100]; // Incorrect length
        new Random().NextBytes(invalidImageData);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new AthenaImageRawUInt8(invalidImageData));
    }
}
