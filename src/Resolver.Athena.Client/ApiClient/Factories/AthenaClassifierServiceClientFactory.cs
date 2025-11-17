using Grpc.Net.Client;
using Resolver.Athena.Client.ApiClient.Interfaces;
using Resolver.Athena.Grpc;

namespace Resolver.Athena.Client.ApiClient.Factories;

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
