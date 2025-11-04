namespace Resolver.Athena.Client.TPL.Options;

/// <summary>
/// Configuration settings for the TPL Dataflow-based streaming client.
/// </summary>
public sealed class AthenaDataflowClientOptions
{
    private int _inputBufferCapacity = 256;
    private int _responseBufferCapacity = 256;
    private int _maxWriteDegreeOfParallelism = 1;

    /// <summary>
    /// Gets or sets the bounded capacity applied to the input buffer.
    /// </summary>
    public int InputBufferCapacity
    {
        get => _inputBufferCapacity;
        set => _inputBufferCapacity = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "Input buffer capacity must be greater than zero.");
    }

    /// <summary>
    /// Gets or sets the bounded capacity applied to the response buffer.
    /// </summary>
    public int ResponseBufferCapacity
    {
        get => _responseBufferCapacity;
        set => _responseBufferCapacity = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "Response buffer capacity must be greater than zero.");
    }

    /// <summary>
    /// Gets or sets the maximum degree of parallelism applied to request writes.
    /// </summary>
    public int MaxWriteDegreeOfParallelism
    {
        get => _maxWriteDegreeOfParallelism;
        set => _maxWriteDegreeOfParallelism = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "Parallelism must be greater than zero.");
    }
}
