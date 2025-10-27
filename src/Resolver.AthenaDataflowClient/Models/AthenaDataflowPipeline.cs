using System.Threading.Tasks.Dataflow;
using Resolver.AthenaClient.Models;

namespace Resolver.AthenaDataflowClient.Models;

/// <summary>
/// Represents a configured TPL Dataflow pipeline for processing Athena classifications.
/// </summary>
public sealed class AthenaDataflowPipeline : IAsyncDisposable
{
    private readonly Func<ValueTask> _disposeAsync;

    internal AthenaDataflowPipeline(
        ITargetBlock<ClassificationRequest> input,
        ISourceBlock<ClassificationResult> output,
        Task completion,
        Func<ValueTask> disposeAsync)
    {
        Input = input;
        Output = output;
        Completion = completion;
        _disposeAsync = disposeAsync;
    }

    /// <summary>
    /// Gets the entry point block for processing requests.
    /// </summary>
    public ITargetBlock<ClassificationRequest> Input { get; }

    /// <summary>
    /// Gets the exit block that produces classification results.
    /// </summary>
    public ISourceBlock<ClassificationResult> Output { get; }

    /// <summary>
    /// Gets a task that completes when the pipeline drains and shuts down.
    /// </summary>
    public Task Completion { get; }

    /// <summary>
    /// Signals that no additional data will be posted to the pipeline.
    /// </summary>
    public void Complete() => Input.Complete();

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _disposeAsync();
}
