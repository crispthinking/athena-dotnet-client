namespace Resolver.AthenaClient.Interfaces;

/// <summary>
/// Contract for creating IAthenaClient instances.
/// </summary>
public interface IAthenaClientFactory
{
    /// <summary>
    /// Creates a new instance of IAthenaClient.
    /// </summary>
    IAthenaClient Create();
}
