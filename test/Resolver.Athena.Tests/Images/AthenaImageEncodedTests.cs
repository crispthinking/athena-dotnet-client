using Resolver.Athena.Client.ApiClient;
using Resolver.Athena.Client.HighLevelClient.Images;
using Resolver.Athena.Grpc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Resolver.Athena.Tests.Images;

public class AthenaImageEncodedTests
{
    [Fact]
    public void Constructor_ValidImageWithIncorrectSize_ResizesAndSetsFormat()
    {
        // Arrange
        var originalWidth = 500;
        var originalHeight = 500;
        using var image = new Image<Rgba32>(originalWidth, originalHeight);
        using var memStream = new MemoryStream();
        image.SaveAsPng(memStream);
        var imageData = memStream.ToArray();

        // Act
        var athenaImage = new AthenaImageEncoded(imageData);

        // Assert
        Assert.Equal(ImageFormat.Png, athenaImage.Format);
        var bytes = athenaImage.GetBytes();
        using var resizedImage = Image.Load(bytes);
        Assert.Equal(AthenaConstants.ExpectedImageWidth, resizedImage.Width);
        Assert.Equal(AthenaConstants.ExpectedImageHeight, resizedImage.Height);
    }

    [Fact]
    public void Constructor_ValidImageWithCorrectSize_SetsFormat()
    {
        // Arrange
        var originalWidth = AthenaConstants.ExpectedImageWidth;
        var originalHeight = AthenaConstants.ExpectedImageHeight;
        using var image = new Image<Rgba32>(originalWidth, originalHeight);
        using var memStream = new MemoryStream();
        image.SaveAsPng(memStream);
        var imageData = memStream.ToArray();

        // Act
        var athenaImage = new AthenaImageEncoded(imageData);

        // Assert
        Assert.Equal(ImageFormat.Png, athenaImage.Format);
        var bytes = athenaImage.GetBytes();
        using var resizedImage = Image.Load(bytes);
        Assert.Equal(AthenaConstants.ExpectedImageWidth, resizedImage.Width);
        Assert.Equal(AthenaConstants.ExpectedImageHeight, resizedImage.Height);
    }

    [Fact]
    public void Constructor_InvalidImage_ThrowsException()
    {
        // Arrange
        var invalidImageData = new byte[] { 0x00, 0x01, 0x02, 0x03 };

        // Act & Assert
        Assert.Throws<UnknownImageFormatException>(() => new AthenaImageEncoded(invalidImageData));
    }
}
