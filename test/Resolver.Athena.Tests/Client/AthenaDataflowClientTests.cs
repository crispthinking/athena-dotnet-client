namespace Resolver.Athena.Tests.Client;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Options;
using Resolver.Athena.Client.ApiClient;
using Resolver.Athena.Client.HighLevelClient.Factories;
using Resolver.Athena.Client.HighLevelClient.Images;
using Resolver.Athena.Client.HighLevelClient.Interfaces;
using Resolver.Athena.Client.HighLevelClient.Models;
using Resolver.Athena.Client.HighLevelClient.Options;
using Resolver.Athena.Client.TPL.Clients;
using Resolver.Athena.Client.TPL.Options;
using Resolver.Athena.Tests.TestSupport;
using Xunit;

/// <summary>
/// Tests for <see cref="AthenaDataflowClient"/>.
/// </summary>
/// <remarks>
/// These tests have an XUnit timeout of 10 seconds, as misbehaving
/// implementations are prone to hanging indefinitely when waiting on results
/// that are never going to appear.
/// </remarks>
public class AthenaDataflowClientTests
{
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

    [Fact(Timeout = 10000)]
    public async Task Pipeline_ProcessesSingleRequestAndResponse()
    {
        // Arrange
        var apiClient = FakeAthenaApiClient.Builder
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

    [Fact(Timeout = 10000)]
    public async Task Pipeline_ProcessesMultipleRequestsAndResponse()
    {
        // Arrange
        var inputCount = 100;

        var builder = FakeAthenaApiClient.Builder;
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

    [Fact(Timeout = 10000)]
    public async Task Pipeline_ProcessesMultipleRequestsAndBatchedResponses()
    {
        // Arrange
        var inputCount = 100;

        var builder = FakeAthenaApiClient.Builder;
        for (var i = 0; i < inputCount / 2; i++)
        {
            builder = builder.WithQueuedResponse(
                    [("ferret", 0.9f), ("guinea pig", 0.1f)],
                    [("ferret", 0.9f), ("guinea pig", 0.1f)]);
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
                var request = new ClassificationRequest($"test-deployment-id-{Guid.NewGuid()}", new AthenaImageRawUInt8(_dummyImageData), $"my-test-correlation-id");
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
            Assert.Equal(2, singleResult.Classifications.Count);
            Assert.Equal("ferret", singleResult.Classifications[0].Label);
            Assert.Equal(0.9f, singleResult.Classifications[0].Confidence);
            Assert.Equal("guinea pig", singleResult.Classifications[1].Label);
            Assert.Equal(0.1f, singleResult.Classifications[1].Confidence);
        }

    }

    [Fact(Timeout = 10000)]
    public async Task Pipeline_ProcessesMultipleRequestsAndResponseWithErrors()
    {
        // Arrange
        var validInputCount = 5;
        var errorInputCount = 3;

        var builder = FakeAthenaApiClient.Builder;
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
