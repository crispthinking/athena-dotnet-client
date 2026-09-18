using Resolver.Athena.Grpc;

namespace Resolver.Athena.Client.HighLevelClient.Images;

/// <summary>
/// Represents an image that is identified only by its hashes. No image data is
/// sent to the API for images of this kind.
/// </summary>
public class AthenaImageHashes : AthenaImageBase
{
    /// <summary>
    /// Creates a new instance of <see cref="AthenaImageHashes"/> from the
    /// provided hashes.
    /// </summary>
    /// <param name="hashes">The hashes identifying the image.</param>
    /// <exception cref="ArgumentException">Thrown when no hashes are provided.</exception>
    public AthenaImageHashes(IEnumerable<ImageHash> hashes) : base(hashes)
    {
        if (Hashes.Count == 0)
        {
            throw new ArgumentException("At least one hash must be provided.", nameof(hashes));
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="AthenaImageHashes"/> from the
    /// provided MD5 and SHA-1 hashes.
    /// </summary>
    /// <param name="md5Hash">The MD5 hash of the image, if any.</param>
    /// <param name="sha1Hash">The SHA-1 hash of the image, if any.</param>
    /// <exception cref="ArgumentException">Thrown when no hashes are provided.</exception>
    public AthenaImageHashes(string? md5Hash = null, string? sha1Hash = null)
        : this(BuildHashes(md5Hash, sha1Hash))
    {
    }

    /// <inheritdoc />
    public override ImageFormat Format => ImageFormat.Unspecified;

    /// <inheritdoc />
    public override bool HasDerivedHashes => false;

    /// <inheritdoc />
    public override Span<byte> GetBytes() => Span<byte>.Empty;

    private static IEnumerable<ImageHash> BuildHashes(string? md5Hash, string? sha1Hash)
    {
        if (!string.IsNullOrWhiteSpace(md5Hash))
        {
            yield return new ImageHash { Type = HashType.Md5, Value = md5Hash };
        }

        if (!string.IsNullOrWhiteSpace(sha1Hash))
        {
            yield return new ImageHash { Type = HashType.Sha1, Value = sha1Hash };
        }
    }
}
