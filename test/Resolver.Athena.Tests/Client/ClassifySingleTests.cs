using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Resolver.Athena.Grpc;
using Resolver.AthenaApiClient;
using Resolver.AthenaClient.Images;
using Resolver.AthenaClient.Models;

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

        _mockLowLevelClient
            .Setup(client => client.ClassifySingleAsync(
                It.IsAny<ClassificationInput>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var classifierOutput = await _athenaClient.ClassifySingleAsync(athenaImage, CancellationToken.None);

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
        var config = new AthenaApiClientConfiguration
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

        _mockLowLevelClient
            .Setup(client => client.ClassifySingleAsync(
                It.IsAny<ClassificationInput>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await athenaClient.ClassifySingleAsync(athenaImage, CancellationToken.None);

        // Assert
        _mockLowLevelClient.Verify(client => client.ClassifySingleAsync(
            It.Is<ClassificationInput>(input =>
                ((sendMd5 ? 1 : 0) == input.Hashes.Count(h => h.Type == HashType.Md5)) &&
                ((sendSha1 ? 1 : 0) == input.Hashes.Count(h => h.Type == HashType.Sha1))
            ),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClassifySingleAsync_WithErrorResponse_PropagatesError()
    {
        // Arrange
        var imageData = new byte[AthenaConstants.ExpectedImageWidth *
                                 AthenaConstants.ExpectedImageHeight *
                                 AthenaConstants.ExpectedImageChannels];
        new Random().NextBytes(imageData);
        var athenaImage = new AthenaImageRawUInt8(imageData);

        var errorResponse = new ClassificationOutput
        {
            CorrelationId = "1",
            Error = new ClassificationError
            {
                Code = ErrorCode.ModelError,
                Message = "Model failed to process the image",
                Details = "Detailed error information"
            }
        };

        _mockLowLevelClient
            .Setup(client => client.ClassifySingleAsync(
                It.IsAny<ClassificationInput>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorResponse);

        // Act
        var classifierOutput = await _athenaClient.ClassifySingleAsync(athenaImage, CancellationToken.None);

        // Assert
        Assert.NotNull(classifierOutput);
        Assert.Equal("1", classifierOutput.CorrelationId);
        Assert.NotNull(classifierOutput.ErrorDetails);
        Assert.Equal(ClassificationErrorCode.ModelError, classifierOutput.ErrorDetails.Code);
        Assert.Equal("Model failed to process the image", classifierOutput.ErrorDetails.Message);
        Assert.Equal("Detailed error information", classifierOutput.ErrorDetails.AdditionalDetails);
    }
}
