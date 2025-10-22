using Grpc.Net.Client;
using Resolver.Athena.Grpc;
using Resolver.Athena.Interfaces;

namespace Resolver.Athena;

/// <summary>
/// Factory for creating ClassifierServiceClient instances.
/// </summary>
public class AthenaClassifierServiceClientFactory : IAthenaClassifierServiceClientFactory
{
    /// <summary>
    /// Creates a new instance of ClassifierServiceClient.
    /// </summary>
    /// <param name="endpoint">The gRPC service endpoint.</param>
    /// <param name="options">The gRPC channel options.</param>
    /// <returns>A new instance of <see cref="ClassifierService.ClassifierServiceClient"/>.</returns>
    public ClassifierService.ClassifierServiceClient Create(string endpoint, GrpcChannelOptions options)
    {
        return new ClassifierService.ClassifierServiceClient(GrpcChannel.ForAddress(endpoint, options));
    }
}
