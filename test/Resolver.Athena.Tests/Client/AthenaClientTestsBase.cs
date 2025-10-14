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
    protected Mock<ITokenManager> _mockTokenManager;
    protected Mock<IAthenaClassifierServiceClientFactory> _mockClassifierClientFactory;
    protected IOptions<AthenaClientConfiguration> _clientOptions;

    public AthenaClientTestsBase()
    {
        _mockGrpcClient = new Mock<ClassifierService.ClassifierServiceClient>();
        _mockClassifierClientFactory = new Mock<IAthenaClassifierServiceClientFactory>();
        _mockClassifierClientFactory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<GrpcChannelOptions>()))
            .Returns(_mockGrpcClient.Object);

        _mockTokenManager = new Mock<ITokenManager>();
        _mockTokenManager.Setup(tm => tm.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("mock-token");

        _clientOptions = new OptionsWrapper<AthenaClientConfiguration>(new AthenaClientConfiguration
        {
            Endpoint = "https://mock-endpoint",
            Affiliate = "test-affiliate",
            SendMd5Hash = true,
            SendSha1Hash = true,
        });

        _athenaClient = new AthenaClient(_mockTokenManager.Object, _clientOptions, _mockClassifierClientFactory.Object);
    }

    public AthenaClient GetAthenaClient(AthenaClientConfiguration config)
    {
        var options = new OptionsWrapper<AthenaClientConfiguration>(config);
        return new AthenaClient(_mockTokenManager.Object, options, _mockClassifierClientFactory.Object);
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
