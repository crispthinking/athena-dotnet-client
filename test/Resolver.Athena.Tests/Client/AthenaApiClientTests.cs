using System.Threading.Channels;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using Moq;
using Resolver.Athena.Grpc;
using Resolver.AthenaApiClient;
using Resolver.AthenaApiClient.Interfaces;

namespace Resolver.Athena.Tests.Client;

public class AthenaApiClientTests()
{
    private readonly List<ClassifyRequest> _sentRequests = [];

    [Fact]
    public async Task SingleSendAndReceiveAsync()
    {
        var response = GenerateResponse([
                [("cat", 0.9f), ("dog", 0.1f)]
        ]);
        var client = CreateTestApiClient([response]);

        var ct = TestContext.Current.CancellationToken;

        var req = new ClassifyRequest();
        req.Inputs.Add(new ClassificationInput()
        {
            Data = ByteString.CopyFromUtf8("test data")
        });

        var channel = await CreateChannelWithDataAsync([req]);

        var outputChannel = await client.ClassifyAsync(channel.Reader, 10, ct);

        channel.Writer.Complete();

        Assert.NotNull(outputChannel);

        List<ClassifyResponse> responses = [];
        await foreach (var resp in outputChannel.Reader.ReadAllAsync(ct))
        {
            responses.Add(resp);
        }

        var actualResponse = Assert.Single(responses);
        Assert.Equal(response, actualResponse);
        var sentRequest = Assert.Single(_sentRequests);
        Assert.Equal(req, sentRequest);
    }

    [Fact]
    public async Task MultipleImagesInSingleRequestSendAndReceiveAsync()
    {
        List<ByteString> images = [
            ByteString.CopyFromUtf8("image data 1"),
            ByteString.CopyFromUtf8("image data 2"),
            ByteString.CopyFromUtf8("image data 3")
        ];

        var response = GenerateResponse([
                [("cat", 0.9f), ("dog", 0.1f)],
                [("bunny", 0.8f), ("frog", 0.2f)],
                [("ferret", 0.7f), ("rat", 0.3f)]
        ]);

        var client = CreateTestApiClient([response]);

        var ct = TestContext.Current.CancellationToken;

        var req = new ClassifyRequest();

        foreach (var img in images)
        {
            req.Inputs.Add(new ClassificationInput()
            {
                Data = img
            });
        }

        var channel = await CreateChannelWithDataAsync([req]);

        var outputChannel = await client.ClassifyAsync(channel.Reader, 10, ct);

        channel.Writer.Complete();

        Assert.NotNull(outputChannel);

        List<ClassifyResponse> responses = [];
        await foreach (var resp in outputChannel.Reader.ReadAllAsync(ct))
        {
            responses.Add(resp);
        }

        var batchResponse = Assert.Single(responses);
        Assert.Equal(response, batchResponse);

        var sentRequest = Assert.Single(_sentRequests);
        Assert.Equal(req, sentRequest);
    }

    [Fact]
    public async Task MultipleImagesInMultipleRequestsSendAndReceiveAsync()
    {
        var dummyResponse = GenerateResponse([
                [("cat", 0.9f), ("dog", 0.1f)],
                [("bunny", 0.8f), ("frog", 0.2f)],
        ]);

        var requests = new List<ClassifyRequest>();
        var responses = new List<ClassifyResponse>();

        for (var i = 0; i < 5; i++)
        {
            var req = new ClassifyRequest();
            req.Inputs.Add(new ClassificationInput()
            {
                Data = ByteString.CopyFromUtf8($"batch {i + 1} image data 1"),
            });
            req.Inputs.Add(new ClassificationInput()
            {
                Data = ByteString.CopyFromUtf8($"batch {i + 1} image data 2"),
            });
            requests.Add(req);

            responses.Add(dummyResponse);
        }

        var client = CreateTestApiClient(responses);

        var ct = TestContext.Current.CancellationToken;

        var channel = await CreateChannelWithDataAsync(requests);

        var outputChannel = await client.ClassifyAsync(channel.Reader, 10, ct);

        channel.Writer.Complete();

        Assert.NotNull(outputChannel);

        List<ClassifyResponse> sentResponses = [];
        await foreach (var resp in outputChannel.Reader.ReadAllAsync(ct))
        {
            sentResponses.Add(resp);
        }

        Assert.Equal(responses.Count, sentResponses.Count);

        Assert.Equal(requests.Count, _sentRequests.Count);
    }

    private static async Task<Channel<ClassifyRequest>> CreateChannelWithDataAsync(IEnumerable<ClassifyRequest> requests)
    {
        var channel = Channel.CreateBounded<ClassifyRequest>(new BoundedChannelOptions(1024)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

        foreach (var request in requests)
        {
            await channel.Writer.WriteAsync(request);
        }

        return channel;
    }

    private AthenaApiClient.Clients.AthenaApiClient CreateTestApiClient(IEnumerable<ClassifyResponse> responses)
    {
        var fakeRequestStream = new Mock<IClientStreamWriter<ClassifyRequest>>();
        fakeRequestStream.Setup(s => s.WriteAsync(It.IsAny<ClassifyRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ClassifyRequest, CancellationToken>((r, ct) => _sentRequests.Add(r))
            .Returns(Task.CompletedTask);
        var fakeResponseStream = new FakeAsyncStreamReader<ClassifyResponse>(responses);

        var duplex = new AsyncDuplexStreamingCall<ClassifyRequest, ClassifyResponse>(
                fakeRequestStream.Object,
                fakeResponseStream,
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => [],
                () => { });

        var mockClient = new Mock<ClassifierService.ClassifierServiceClient>();
        mockClient.Setup(c => c.Classify(It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(duplex);

        var opts = new OptionsWrapper<AthenaApiClientConfiguration>(new AthenaApiClientConfiguration
        {
            Endpoint = "https://mock-endpoint",
            Affiliate = "test-affiliate",
            SendMd5Hash = true,
            SendSha1Hash = true,
        });
        var tokenManagerMock = new Mock<ITokenManager>();
        tokenManagerMock.Setup(tm => tm.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("mock-token");

        var factory = new Mock<IAthenaClassifierServiceClientFactory>();
        factory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<GrpcChannelOptions>()))
            .Returns(mockClient.Object);

        return new AthenaApiClient.Clients.AthenaApiClient(tokenManagerMock.Object, opts, factory.Object);
    }

    private static ClassifyResponse GenerateResponse(List<List<(string, float)>> classificationsPerOutput)
    {
        var response = new ClassifyResponse();
        foreach (var classificationOutput in classificationsPerOutput)
        {
            var output = new ClassificationOutput();
            foreach ((var label, var weight) in classificationOutput)
            {
                output.Classifications.Add(new Classification
                {
                    Label = label,
                    Weight = weight
                });
            }

            response.Outputs.Add(output);
        }

        return response;
    }
}

internal class FakeAsyncStreamReader<T>(IEnumerable<T> items) : IAsyncStreamReader<T>
{
    private readonly IEnumerator<T> _enumerator = items.GetEnumerator();

    public T Current => _enumerator.Current;

    public Task<bool> MoveNext(CancellationToken cancellationToken)
        => Task.FromResult(_enumerator.MoveNext());
}
