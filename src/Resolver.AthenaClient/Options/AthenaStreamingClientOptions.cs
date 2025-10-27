namespace Resolver.AthenaClient.Options;

/// <summary>
/// Configuration settings for the Athena Client.
/// </summary>
public sealed class AthenaClientOptions
{
    private int _responseChannelCapacity = 256;

    /// <summary>
    /// Gets or sets the capacity of the channel used to buffer results.
    /// </summary>
    public int ResponseChannelCapacity
    {
        get => _responseChannelCapacity;
        set => _responseChannelCapacity = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "Response channel capacity must be greater than zero.");
    }

    /// <summary>
    /// Gets or sets the delegate used to generate correlation identifiers when one is not supplied.
    /// </summary>
    public Func<string> CorrelationIdFactory { get; set; } = static () => Guid.NewGuid().ToString("N");
}
