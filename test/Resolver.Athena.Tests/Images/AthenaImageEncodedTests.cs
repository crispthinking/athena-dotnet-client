using Resolver.Athena.Client.ApiClient;
using Resolver.Athena.Client.HighLevelClient.Images;
using Resolver.Athena.Grpc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;

namespace Resolver.Athena.Tests.Images;

public class AthenaImageEncodedTests
{
    private class SupportedImageEncoders : TheoryData<IImageEncoder>
    {
        public SupportedImageEncoders()
        {
            Add(new SixLabors.ImageSharp.Formats.Gif.GifEncoder());
            Add(new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
            Add(new SixLabors.ImageSharp.Formats.Bmp.BmpEncoder());
            Add(new SixLabors.ImageSharp.Formats.Png.PngEncoder());
            Add(new SixLabors.ImageSharp.Formats.Webp.WebpEncoder());
            Add(new SixLabors.ImageSharp.Formats.Pbm.PbmEncoder());
            Add(new SixLabors.ImageSharp.Formats.Tiff.TiffEncoder());
        }
    }

    [Theory]
    [ClassData(typeof(SupportedImageEncoders))]
    public void Constructor_ValidImageWithIncorrectSize_ResizesAndSetsFormat(IImageEncoder encoder)
    {
        // Arrange
        var originalWidth = 500;
        var originalHeight = 500;
        using var image = new Image<Rgba32>(originalWidth, originalHeight);
        using var memStream = new MemoryStream();
        image.Save(memStream, encoder);
        var imageData = memStream.ToArray();

        // Act
        var athenaImage = new AthenaImageEncoded(imageData);

        // Assert
        Assert.Equal(ImageFormat.RawUint8Bgr, athenaImage.Format);
        var bytes = athenaImage.GetBytes();
        Assert.Equal(AthenaConstants.ExpectedImageWidth * AthenaConstants.ExpectedImageHeight * AthenaConstants.ExpectedImageChannels, bytes.Length);
    }

    [Theory]
    [ClassData(typeof(SupportedImageEncoders))]
    public void Constructor_ValidImageWithCorrectSize_SetsFormat(IImageEncoder encoder)
    {
        // Arrange
        var originalWidth = AthenaConstants.ExpectedImageWidth;
        var originalHeight = AthenaConstants.ExpectedImageHeight;
        using var image = new Image<Rgba32>(originalWidth, originalHeight);
        using var memStream = new MemoryStream();
        image.Save(memStream, encoder);
        var imageData = memStream.ToArray();

        // Act
        var athenaImage = new AthenaImageEncoded(imageData);

        // Assert
        Assert.Equal(ImageFormat.RawUint8Bgr, athenaImage.Format);
        var bytes = athenaImage.GetBytes();
        Assert.Equal(AthenaConstants.ExpectedImageWidth * AthenaConstants.ExpectedImageHeight * AthenaConstants.ExpectedImageChannels, bytes.Length);
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
