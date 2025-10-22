using Grpc.Net.Client;
using Resolver.Athena.Grpc;

namespace Resolver.Athena.Interfaces;

/// <summary>
/// Contract for creating ClassifierServiceClient instances.
/// </summary>
public interface IAthenaClassifierServiceClientFactory
{
    /// <summary>
    /// Creates a new instance of ClassifierServiceClient.
    /// </summary>
    ClassifierService.ClassifierServiceClient Create(string endpoint, GrpcChannelOptions options);
}
