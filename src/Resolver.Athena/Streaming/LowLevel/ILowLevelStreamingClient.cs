using Resolver.Athena.Grpc;

namespace Resolver.Athena.Streaming.LowLevel;

public interface ILowLevelStreamingClient
{
    IAsyncEnumerable<ClassifyResponse> GetResponsesAsync(CancellationToken cancellationToken);
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
