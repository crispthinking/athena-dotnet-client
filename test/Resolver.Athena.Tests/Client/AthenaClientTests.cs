using Microsoft.Extensions.Options;
using Resolver.Athena.Tests.TestSupport;
using Resolver.AthenaApiClient;
using Resolver.AthenaClient.Factories;
using Resolver.AthenaClient.Images;
using Resolver.AthenaClient.Interfaces;
using Resolver.AthenaClient.Models;
using Resolver.AthenaClient.Options;

namespace Resolver.Athena.Tests.Client;

public class AthenaClientTests()
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
    private readonly byte[] _dummyImageData = new byte[AthenaConstants.ExpectedImageWidth * AthenaConstants.ExpectedImageHeight * AthenaConstants.ExpectedImageChannels];

    [Fact(Timeout = 10000)]
    public async Task ProcessesSingleRequest_ReturnsSingleResponse()
    {
        var apiClient = FakeAthenaApiClient.Builder
            .WithQueuedResponse([("ferret", 0.95f), ("ermine", 0.05f)])
            .Build();

        var athenaClient = new AthenaClient.AthenaClient(apiClient, _inputFactory, _streamingOptions);
        List<ClassificationRequest> requests = [new ClassificationRequest("test-deployment-id", new AthenaImageRawUInt8(_dummyImageData), "my-correlation-id")];

        var results = athenaClient.ClassifyAsync(ToAsyncEnumerable(requests), TestContext.Current.CancellationToken);

        List<ClassificationResult> allResults = [];
        await foreach (var result in results)
        {
            allResults.Add(result);
            break; // only expecting one result
        }

        var singleResult = Assert.Single(allResults);
        Assert.Equal("my-correlation-id", singleResult.CorrelationId);
        Assert.Equal(2, singleResult.Classifications.Count);
        Assert.Equal("ferret", singleResult.Classifications[0].Label);
        Assert.Equal(0.95f, singleResult.Classifications[0].Confidence);
        Assert.Equal("ermine", singleResult.Classifications[1].Label);
        Assert.Equal(0.05f, singleResult.Classifications[1].Confidence);
    }

    [Fact(Timeout = 10000)]
    public async Task ProcessesManyRequests_ReturnsManyResponses()
    {
        var requestsToSend = 100;
        var builder = FakeAthenaApiClient.Builder;

        for (var i = 0; i < requestsToSend; i++)
        {
            builder = builder.WithQueuedResponse([("stoat", 0.6f), ("mink", 0.4f)]);
        }

        var apiClient = builder.Build();

        var athenaClient = new AthenaClient.AthenaClient(apiClient, _inputFactory, _streamingOptions);
        List<ClassificationRequest> requests = [.. Enumerable.Range(0, requestsToSend).Select(i =>
            new ClassificationRequest("test-deployment-id", new AthenaImageRawUInt8(_dummyImageData), $"my-correlation-id-{i}")
        )];

        var results = athenaClient.ClassifyAsync(ToAsyncEnumerable(requests), TestContext.Current.CancellationToken);

        List<ClassificationResult> allResults = [];
        await foreach (var result in results)
        {
            allResults.Add(result);
            if (allResults.Count >= requestsToSend)
            {
                break; // received all expected results
            }
        }

        Assert.Equal(requestsToSend, allResults.Count);
        foreach (var result in allResults)
        {
            Assert.StartsWith("my-correlation-id", result.CorrelationId);
            Assert.Equal(2, result.Classifications.Count);
            Assert.Equal("stoat", result.Classifications[0].Label);
            Assert.Equal(0.6f, result.Classifications[0].Confidence);
            Assert.Equal("mink", result.Classifications[1].Label);
            Assert.Equal(0.4f, result.Classifications[1].Confidence);
        }
    }

    private static async IAsyncEnumerable<ClassificationRequest> ToAsyncEnumerable(IEnumerable<ClassificationRequest> requests)
    {
        foreach (var request in requests)
        {
            yield return request;
            await Task.Yield(); // simulate async behavior
        }
    }
}
