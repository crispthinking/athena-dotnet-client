using Resolver.Athena.Images;

namespace Resolver.Athena.LowLevel;

public interface IPreBatchedLowLevelStreamingClient : ILowLevelStreamingClient
{
    Task SendBatchAsync(AthenaImageBase[] batch, CancellationToken cancellationToken);
}
