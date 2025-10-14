using Grpc.Net.Client;
using Resolver.Athena.Grpc;
using Resolver.Athena.Interfaces;

namespace Resolver.Athena;

public class AthenaClassifierServiceClientFactory : IAthenaClassifierServiceClientFactory
{
    public ClassifierService.ClassifierServiceClient Create(string endpoint, GrpcChannelOptions options)
    {
        return new ClassifierService.ClassifierServiceClient(GrpcChannel.ForAddress(endpoint, options));
    }
}
