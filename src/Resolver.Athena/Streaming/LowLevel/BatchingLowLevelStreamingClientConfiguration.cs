namespace Resolver.Athena.Streaming.LowLevel;

public class BatchingLowLevelStreamingClientConfiguration : LowLevelStreamingClientConfiguration
{
    public int ChannelCapacity { get; set; } = 100;
    public int MaxBatchSize { get; set; } = 10;
}
