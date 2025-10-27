using Resolver.AthenaDataflowClient.Models;

namespace Resolver.AthenaDataflowClient.Interfaces;

/// <summary>
/// Athena TPL dataflow client interface.
/// </summary>
public interface IAthenaDataflowClient
{
    /// <summary>
    /// Creates a new pipeline that accepts streaming requests and produces classification results.
    /// </summary>
    Task<AthenaDataflowPipeline> CreatePipelineAsync(CancellationToken cancellationToken = default);
}
