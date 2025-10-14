using Grpc.Core;
using Moq;
using Resolver.Athena.Grpc;
using Resolver.Athena.Images;

namespace Resolver.Athena.Tests.Client;

public class ClassifySingleTests : AthenaClientTestsBase
{
    [Fact]
    public async Task ClassifySingleAsync_ValidImage_ReturnsClassifications()
    {
        // Arrange
        var imageData = new byte[AthenaConstants.ExpectedImageWidth *
                                 AthenaConstants.ExpectedImageHeight *
                                 AthenaConstants.ExpectedImageChannels];
        new Random().NextBytes(imageData);
        var athenaImage = new AthenaImageRawUInt8(imageData);

        var classification = new Classification
        {
            Label = "cat",
            Weight = 0.95f
        };

        var response = new ClassificationOutput
        {
            CorrelationId = "1",
            Classifications = { classification }
        };

        _mockGrpcClient
            .Setup(client => client.ClassifySingleAsync(
                It.IsAny<ClassificationInput>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(response));

        // Act
        var classifierOutput = await _athenaClient.ClassifySingleImageAsync(athenaImage, CancellationToken.None);

        // Assert
        Assert.NotNull(classifierOutput);
        Assert.Equal("1", classifierOutput.CorrelationId);
        var singleClassification = Assert.Single(classifierOutput.Classifications);
        Assert.Equal("cat", singleClassification.Label);
        Assert.Equal(0.95f, singleClassification.Confidence);
    }

    [Theory, CombinatorialData]
    public async Task ClassifySingleAsync_WithHashOptions_ComputesHashesCorrectly(bool sendMd5, bool sendSha1)
    {
        // Arrange
        var config = new AthenaClientConfiguration
        {
            Endpoint = "https://mock-endpoint",
            Affiliate = "test-affiliate",
            SendMd5Hash = sendMd5,
            SendSha1Hash = sendSha1,
        };
        var athenaClient = GetAthenaClient(config);
        var imageData = new byte[AthenaConstants.ExpectedImageWidth *
                                 AthenaConstants.ExpectedImageHeight *
                                 AthenaConstants.ExpectedImageChannels];
        new Random().NextBytes(imageData);
        var athenaImage = new AthenaImageRawUInt8(imageData);

        var response = new ClassificationOutput
        {
            CorrelationId = "1",
            Classifications = { new Classification { Label = "dog", Weight = 0.85f } }
        };

        _mockGrpcClient
            .Setup(client => client.ClassifySingleAsync(
                It.IsAny<ClassificationInput>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(response));

        // Act
        await athenaClient.ClassifySingleImageAsync(athenaImage, CancellationToken.None);

        // Assert
        _mockGrpcClient.Verify(client => client.ClassifySingleAsync(
            It.Is<ClassificationInput>(input =>
                ((sendMd5 ? 1 : 0) == input.Hashes.Count(h => h.Type == HashType.Md5)) &&
                ((sendSha1 ? 1 : 0) == input.Hashes.Count(h => h.Type == HashType.Sha1))
            ),
            It.IsAny<Metadata>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
