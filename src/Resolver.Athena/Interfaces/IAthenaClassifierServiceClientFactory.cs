using Grpc.Net.Client;
using Resolver.Athena.Grpc;

namespace Resolver.Athena.Interfaces;

public interface IAthenaClassifierServiceClientFactory
{
    ClassifierService.ClassifierServiceClient Create(string endpoint, GrpcChannelOptions options);
}
