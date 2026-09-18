using Microsoft.Extensions.Options;
using Resolver.Athena.Client.ApiClient;
using Resolver.Athena.Client.HighLevelClient.Factories;
using Resolver.Athena.Client.HighLevelClient.Images;
using Resolver.Athena.Grpc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Resolver.Athena.Tests.Factories;

public class AthenaClassificationInputFactoryTests
{
    private static AthenaClassificationInputFactory CreateFactory(bool sendMd5, bool sendSha1) =>
        new(new OptionsWrapper<AthenaApiClientConfiguration>(new AthenaApiClientConfiguration
        {
            Affiliate = "test-affiliate",
            SendMd5Hash = sendMd5,
            SendSha1Hash = sendSha1,
        }));

    [Fact]
    public void Create_EncodedImage_UsesHashesOfOriginalImage()
    {
        // Arrange
        using var image = new Image<Rgba32>(500, 500);
        using var memStream = new MemoryStream();
        image.Save(memStream, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
        var imageData = memStream.ToArray();
        var athenaImage = new AthenaImageEncoded(imageData);
        var factory = CreateFactory(sendMd5: true, sendSha1: true);

        // Act
        var input = factory.Create(athenaImage);

        // Assert
        Assert.Equal(athenaImage.GetMd5Hash(), input.Hashes.Single(h => h.Type == HashType.Md5).Value);
        Assert.Equal(athenaImage.GetSha1Hash(), input.Hashes.Single(h => h.Type == HashType.Sha1).Value);
    }

    [Fact]
    public void Create_HashOnlyImage_AlwaysSendsHashesWithoutData()
    {
        // Arrange
        var athenaImage = new AthenaImageHashes(md5Hash: "abc", sha1Hash: "def");
        var factory = CreateFactory(sendMd5: false, sendSha1: false);

        // Act
        var input = factory.Create(athenaImage);

        // Assert
        Assert.Empty(input.Data.ToByteArray());
        Assert.Equal(ImageFormat.Unspecified, input.Format);
        Assert.Equal("abc", input.Hashes.Single(h => h.Type == HashType.Md5).Value);
        Assert.Equal("def", input.Hashes.Single(h => h.Type == HashType.Sha1).Value);
    }
}
