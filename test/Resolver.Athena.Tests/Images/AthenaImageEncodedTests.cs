using Resolver.Athena.Client.ApiClient;
using Resolver.Athena.Client.HighLevelClient.Images;
using Resolver.Athena.Grpc;
using OpenCvSharp;


namespace Resolver.Athena.Tests.Images;

public class AthenaImageEncodedTests
{
    /// <summary>
    /// Image encodings whose output OpenCV can reliably decode via ImDecode.
    /// </summary>
    private class OpenCvCompatibleExtensions : TheoryData<string>
    {
        public OpenCvCompatibleExtensions()
        {
            Add(".jpg");
            Add(".png");
            Add(".webp");
            Add(".tiff");
            Add(".bmp");
        }
    }

    [Theory]
    [ClassData(typeof(OpenCvCompatibleExtensions))]
    public void Constructor_ValidImageWithIncorrectSize_ResizesAndSetsFormat(string extension)
    {
        // Arrange
        var originalWidth = 500;
        var originalHeight = 500;
        using var image = new Mat(new Size(originalWidth, originalHeight), MatType.CV_8UC3, Scalar.All(128));
        Cv2.ImEncode(extension, image, out var imageData);

        // Act
        var athenaImage = new AthenaImageEncoded(imageData);

        // Assert
        Assert.Equal(ImageFormat.RawUint8Bgr, athenaImage.Format);
        var bytes = athenaImage.GetBytes();
        Assert.Equal(AthenaConstants.ExpectedImageWidth * AthenaConstants.ExpectedImageHeight * AthenaConstants.ExpectedImageChannels, bytes.Length);
    }

    [Theory]
    [ClassData(typeof(OpenCvCompatibleExtensions))]
    public void Constructor_ValidImageWithCorrectSize_SetsFormat(string extension)
    {
        // Arrange
        var originalWidth = AthenaConstants.ExpectedImageWidth;
        var originalHeight = AthenaConstants.ExpectedImageHeight;
        using var image = new Mat(new Size(originalWidth, originalHeight), MatType.CV_8UC3, Scalar.All(128));
        Cv2.ImEncode(extension, image, out var imageData);

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
        Assert.Throws<FormatException>(() => new AthenaImageEncoded(invalidImageData));
    }
}
