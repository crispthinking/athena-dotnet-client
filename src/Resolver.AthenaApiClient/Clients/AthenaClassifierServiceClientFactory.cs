using Grpc.Net.Client;
using Resolver.Athena.Grpc;
using Resolver.AthenaApiClient.Interfaces;

namespace Resolver.AthenaApiClient.Clients;

/// <summary>
/// Factory for creating ClassifierServiceClient instances.
/// </summary>
public class AthenaClassifierServiceClientFactory : IAthenaClassifierServiceClientFactory
{
    /// <inheritdoc />
    public ClassifierService.ClassifierServiceClient Create(string endpoint, GrpcChannelOptions options)
    {
        return new ClassifierService.ClassifierServiceClient(GrpcChannel.ForAddress(endpoint, options));
    }
}
