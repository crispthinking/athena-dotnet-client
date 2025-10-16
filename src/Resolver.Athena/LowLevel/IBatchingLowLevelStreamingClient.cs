using Resolver.Athena.Images;

namespace Resolver.Athena.LowLevel;

public interface IBatchingLowLevelStreamingClient
{
    Task SendAsync(AthenaImageBase imageData, CancellationToken cancellationToken);
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
