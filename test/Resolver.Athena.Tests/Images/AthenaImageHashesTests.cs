using Resolver.Athena.Client.HighLevelClient.Images;
using Resolver.Athena.Grpc;

namespace Resolver.Athena.Tests.Images;

public class AthenaImageHashesTests
{
    [Fact]
    public void Constructor_WithHashValues_ExposesHashesAndNoData()
    {
        // Act
        var athenaImage = new AthenaImageHashes(md5Hash: "abc", sha1Hash: "def");

        // Assert
        Assert.Equal(ImageFormat.Unspecified, athenaImage.Format);
        Assert.False(athenaImage.HasDerivedHashes);
        Assert.Equal(0, athenaImage.GetBytes().Length);
        Assert.Equal("abc", athenaImage.GetMd5Hash());
        Assert.Equal("def", athenaImage.GetSha1Hash());
    }

    [Fact]
    public void Constructor_WithSingleHash_OmitsMissingHash()
    {
        // Act
        var athenaImage = new AthenaImageHashes(sha1Hash: "def");

        // Assert
        var hash = Assert.Single(athenaImage.Hashes);
        Assert.Equal(HashType.Sha1, hash.Type);
        Assert.Null(athenaImage.GetMd5Hash());
    }

    [Fact]
    public void Constructor_WithNoHashes_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new AthenaImageHashes());
        Assert.Throws<ArgumentException>(() => new AthenaImageHashes([]));
    }
}
