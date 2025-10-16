using Resolver.Athena.Grpc;

namespace Resolver.Athena.LowLevel;

public interface IPreBatchedLowLevelStreamingClient
{
    Task SendBatchAsync(ClassifyRequest request, CancellationToken cancellationToken);
}
