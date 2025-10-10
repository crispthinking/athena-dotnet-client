using Resolver.Athena.Grpc;
using Moq;
using Grpc.Core;

namespace Resolver.Athena.Tests.AthenaClientTests;

public class AthenaClientTestsBase
{
    protected AthenaClient _athenaClient;
    protected Mock<ClassifierService.ClassifierServiceClient> _mockGrpcClient;

    public AthenaClientTestsBase()
    {
        _mockGrpcClient = new Mock<ClassifierService.ClassifierServiceClient>();
        _athenaClient = new AthenaClient(_mockGrpcClient.Object);
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

