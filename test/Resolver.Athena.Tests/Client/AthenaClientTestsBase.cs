using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using Moq;
using Resolver.Athena.Grpc;
using Resolver.Athena.Interfaces;

namespace Resolver.Athena.Tests.Client;

public class AthenaClientTestsBase
{
    protected AthenaClient _athenaClient;
    protected Mock<ClassifierService.ClassifierServiceClient> _mockGrpcClient;

    public AthenaClientTestsBase()
    {
        _mockGrpcClient = new Mock<ClassifierService.ClassifierServiceClient>();
        var mockClassifierClientFactory = new Mock<IAthenaClassifierServiceClientFactory>();
        mockClassifierClientFactory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<GrpcChannelOptions>()))
            .Returns(_mockGrpcClient.Object);

        var mockTokenManager = new Mock<ITokenManager>();
        mockTokenManager.Setup(tm => tm.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("mock-token");

        var mockOptions = new Mock<IOptions<AthenaClientConfiguration>>();
        mockOptions.Setup(o => o.Value).Returns(new AthenaClientConfiguration
        {
            Endpoint = "https://mock-endpoint",
            Affiliate = "test-affiliate",
        });

        _athenaClient = new AthenaClient(mockTokenManager.Object, mockOptions.Object, mockClassifierClientFactory.Object);
    }

    public static AsyncUnaryCall<TResponse> CreateAsyncUnaryCall<TResponse>(TResponse response)
        where TResponse : class
    {
        return new AsyncUnaryCall<TResponse>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => [],
            () => { });
    }
}
