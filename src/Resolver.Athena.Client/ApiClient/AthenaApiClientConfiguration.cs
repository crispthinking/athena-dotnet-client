namespace Resolver.Athena.Client.ApiClient;

/// <summary>
/// Configuration settings for the Athena client.
/// </summary>
public class AthenaApiClientConfiguration
{
    /// <summary>
    /// The default Athena API endpoint.
    /// </summary>
    public const string DefaultEndpoint = "https://api.athena-risk-intelligence.com/";

    /// <summary>
    /// The Athena service endpoint. Defaults to <see cref="DefaultEndpoint"/>.
    /// </summary>
    public string Endpoint { get; set; } = DefaultEndpoint;

    /// <summary>
    /// The affiliate identifier.
    /// </summary>
    public required string Affiliate { get; set; }

    /// <summary>
    /// Indicates whether to send MD5 hashes of images.
    /// </summary>
    public bool SendMd5Hash { get; set; }

    /// <summary>
    /// Indicates whether to send SHA1 hashes of images.
    /// </summary>
    public bool SendSha1Hash { get; set; }

    /// <summary>
    /// Indicates whether to allow insecure connections.
    /// </summary>
    /// <remarks>
    /// This is obviously unsuitable for production use, and is intended for
    /// local development and testing only.
    /// </remarks>
    public bool UnsafeAllowInsecure { get; set; }
}
