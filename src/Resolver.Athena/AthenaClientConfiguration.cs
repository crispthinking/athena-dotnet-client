namespace Resolver.Athena;

/// <summary>
/// Configuration settings for the Athena client.
/// </summary>
public class AthenaClientConfiguration
{
    /// <summary>
    /// The Athena service endpoint.
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// The affiliate identifier.
    /// </summary>
    public required string Affiliate { get; set; }

    /// <summary>
    /// Indicates whether to send MD5 hashes of images.
    /// </summary>
    public bool SendMd5Hash { get; set; } = false;

    /// <summary>
    /// Indicates whether to send SHA1 hashes of images.
    /// </summary>
    public bool SendSha1Hash { get; set; } = false;
}
