namespace Resolver.Athena.Tests.Client;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Options;
using Moq;
using Resolver.Athena.Grpc;
using Resolver.Athena.Tests.TestSupport;
using Resolver.AthenaApiClient;
using Resolver.AthenaApiClient.Interfaces;
using Resolver.AthenaClient.Factories;
using Resolver.AthenaClient.Images;
using Resolver.AthenaClient.Interfaces;
using Resolver.AthenaClient.Models;
using Resolver.AthenaClient.Options;
using Resolver.AthenaDataflowClient.Clients;
using Resolver.AthenaDataflowClient.Options;
using Xunit;

public class AthenaDataflowClientTests
{
    private readonly Mock<IAthenaApiClient> _apiClientMock = new();
    private readonly IAthenaClassificationInputFactory _inputFactory = new AthenaClassificationInputFactory(Options.Create(new AthenaApiClientConfiguration
    {
        Affiliate = "test-affiliate",
        Endpoint = "http://test-endpoint",
        SendMd5Hash = true,
        SendSha1Hash = true
    }));
    private readonly IOptions<AthenaClientOptions> _streamingOptions =
        Options.Create(new AthenaClientOptions
        {
            RequestChannelCapacity = 10,
            ResponseChannelCapacity = 10,
            CorrelationIdFactory = () => Guid.NewGuid().ToString()
        });
    private readonly IOptions<AthenaDataflowClientOptions> _dataflowOptions =
        Options.Create(new AthenaDataflowClientOptions
        {
            ResponseBufferCapacity = 10,
            InputBufferCapacity = 10,
            MaxWriteDegreeOfParallelism = 2
        });
    private readonly byte[] _dummyImageData = new byte[AthenaConstants.ExpectedImageWidth * AthenaConstants.ExpectedImageHeight * AthenaConstants.ExpectedImageChannels];

    private AthenaDataflowClient CreateClient()
    {
        return new AthenaDataflowClient(
            _apiClientMock.Object,
            _inputFactory,
            _streamingOptions,
            _dataflowOptions);
    }

    [Fact]
    public async Task CreatePipelineAsync_ReturnsPipeline()
    {
        // Arrange
        var responseChannel = Channel.CreateUnbounded<ClassifyResponse>();
        _apiClientMock
            .Setup(x => x.ClassifyAsync(It.IsAny<ChannelReader<ClassifyRequest>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseChannel);

        // Act
        var client = CreateClient();
        var pipeline = await client.CreatePipelineAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(pipeline.Input);
        Assert.NotNull(pipeline.Output);
        Assert.NotNull(pipeline.Completion);
    }

    [Fact]
    public async Task Pipeline_ProcessesSingleRequestAndResponse()
    {
        // Arrange
        var apiClient = new FakeAthenaApiClient.FakeAthenaApiClientBuilder()
            .WithQueuedResponse([("ferret", 0.9f), ("guinea pig", 0.1f)])
            .Build();

        var client = new AthenaDataflowClient(
            apiClient,
            _inputFactory,
            _streamingOptions,
            _dataflowOptions);

        var pipeline = await client.CreatePipelineAsync(TestContext.Current.CancellationToken);

        // Act

        var request = new ClassificationRequest("test-id", new AthenaImageRawUInt8(_dummyImageData), "my-test-correlation-id");

        await pipeline.Input.SendAsync(request, TestContext.Current.CancellationToken);
        pipeline.Input.Complete();

        var singleResult = await pipeline.Output.ReceiveAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("my-test-correlation-id", singleResult.CorrelationId);
        Assert.Equal(2, singleResult.Classifications.Count);
        Assert.Equal("ferret", singleResult.Classifications[0].Label);
        Assert.Equal(0.9f, singleResult.Classifications[0].Confidence);
        Assert.Equal("guinea pig", singleResult.Classifications[1].Label);
        Assert.Equal(0.1f, singleResult.Classifications[1].Confidence);
    }

    [Fact]
    public async Task Pipeline_ProcessesMultipleRequestsAndResponse()
    {
        // Arrange
        var inputCount = 100;

        var builder = new FakeAthenaApiClient.FakeAthenaApiClientBuilder();
        for (var i = 0; i < inputCount; i++)
        {
            builder = builder.WithQueuedResponse([("ferret", 0.9f), ("guinea pig", 0.1f)]);
        }

        var apiClient = builder.Build();

        var client = new AthenaDataflowClient(
            apiClient,
            _inputFactory,
            _streamingOptions,
            _dataflowOptions);

        var pipeline = await client.CreatePipelineAsync(TestContext.Current.CancellationToken);

        // Act

        var senderTask = Task.Run(async () =>
        {
            for (var i = 0; i < inputCount; i++)
            {
                var request = new ClassificationRequest("test-deployment-id", new AthenaImageRawUInt8(_dummyImageData), $"my-test-correlation-id");
                await pipeline.Input.SendAsync(request, TestContext.Current.CancellationToken);
            }

            pipeline.Input.Complete();
        }, TestContext.Current.CancellationToken);

        var results = new List<ClassificationResult>();

        var receiverTask = Task.Run(async () =>
        {
            for (var i = 0; i < inputCount; i++)
            {
                var singleResult = await pipeline.Output.ReceiveAsync(TestContext.Current.CancellationToken);
                results.Add(singleResult);
            }
        }, TestContext.Current.CancellationToken);

        await Task.WhenAll(senderTask, receiverTask);

        // Assert

        Assert.Equal(inputCount, results.Count);
        foreach (var singleResult in results)
        {
            Assert.Equal("my-test-correlation-id", singleResult.CorrelationId);
            Assert.Equal(2, singleResult.Classifications.Count);
            Assert.Equal("ferret", singleResult.Classifications[0].Label);
            Assert.Equal(0.9f, singleResult.Classifications[0].Confidence);
            Assert.Equal("guinea pig", singleResult.Classifications[1].Label);
            Assert.Equal(0.1f, singleResult.Classifications[1].Confidence);
        }

    }

    [Fact]
    public async Task Pipeline_ProcessesMultipleRequestsAndResponseWithErrors()
    {
        // Arrange
        var validInputCount = 5;
        var errorInputCount = 3;

        var builder = new FakeAthenaApiClient.FakeAthenaApiClientBuilder();
        for (var i = 0; i < validInputCount; i++)
        {
            builder = builder.WithQueuedResponse([("ferret", 0.9f), ("guinea pig", 0.1f)]);
        }

        for (var i = 0; i < errorInputCount; i++)
        {
            builder = builder.WithQueuedErrorResponse("Simulated error");
        }

        var apiClient = builder.Build();

        var client = new AthenaDataflowClient(
            apiClient,
            _inputFactory,
            _streamingOptions,
            _dataflowOptions);

        var pipeline = await client.CreatePipelineAsync(TestContext.Current.CancellationToken);

        // Act

        var senderTask = Task.Run(async () =>
        {
            for (var i = 0; i < validInputCount + errorInputCount; i++)
            {
                var request = new ClassificationRequest("test-deployment-id", new AthenaImageRawUInt8(_dummyImageData), $"my-test-correlation-id");
                await pipeline.Input.SendAsync(request, TestContext.Current.CancellationToken);
            }

            pipeline.Input.Complete();
        }, TestContext.Current.CancellationToken);

        var results = new List<ClassificationResult>();

        var receiverTask = Task.Run(async () =>
        {
            for (var i = 0; i < validInputCount + errorInputCount; i++)
            {
                var singleResult = await pipeline.Output.ReceiveAsync(TestContext.Current.CancellationToken);
                results.Add(singleResult);
            }
        }, TestContext.Current.CancellationToken);

        await Task.WhenAll(senderTask, receiverTask);

        // Assert

        Assert.Equal(validInputCount + errorInputCount, results.Count);

        var validResults = results.Where(r => r.ErrorDetails == null);
        Assert.Equal(validInputCount, validResults.Count());

        foreach (var singleResult in validResults)
        {
            Assert.Equal("my-test-correlation-id", singleResult.CorrelationId);
            Assert.Equal(2, singleResult.Classifications.Count);
            Assert.Equal("ferret", singleResult.Classifications[0].Label);
            Assert.Equal(0.9f, singleResult.Classifications[0].Confidence);
            Assert.Equal("guinea pig", singleResult.Classifications[1].Label);
            Assert.Equal(0.1f, singleResult.Classifications[1].Confidence);
        }

        var errorResults = results.Where(r => r.ErrorDetails != null);
        Assert.Equal(errorInputCount, errorResults.Count());
        foreach (var singleResult in errorResults)
        {
            Assert.Equal("Simulated error", singleResult?.ErrorDetails?.Message);
        }

    }
}
