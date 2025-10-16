namespace Resolver.Athena.LowLevel;

public class LowLevelStreamingConfiguration
{
    public required string Endpoint { get; set; }
    public required string DeploymentId { get; set; }

}

public class LowLevelStreamingWithBatchingConfiguration : LowLevelStreamingConfiguration
{
    public int ChannelCapacity { get; set; } = 100;
    public int MaxBatchSize { get; set; } = 10;
}
