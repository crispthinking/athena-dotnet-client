using Resolver.Athena.Images;

namespace Resolver.Athena.Streaming.LowLevel;

public interface IBatchingLowLevelStreamingClient : ILowLevelStreamingClient
{
    Task SendAsync(AthenaImageBase imageData, CancellationToken cancellationToken);
}
