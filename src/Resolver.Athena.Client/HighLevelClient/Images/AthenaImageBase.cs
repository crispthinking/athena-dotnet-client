using System.Security.Cryptography;
using Resolver.Athena.Grpc;

namespace Resolver.Athena.Client.HighLevelClient.Images;

/// <summary>
/// Base class for Athena image representations.
/// </summary>
public abstract class AthenaImageBase
{
    private readonly ImageHash[] _hashes;

    /// <summary>
    /// Initializes a new image, computing the MD5 and SHA-1 hashes of the
    /// supplied data.
    /// </summary>
    /// <param name="hashSource">
    /// The data to hash. This should always be the original image data, before
    /// any transformation is applied to it.
    /// </param>
    protected AthenaImageBase(ReadOnlySpan<byte> hashSource)
    {
        _hashes =
        [
            new ImageHash
            {
                Type = HashType.Md5,
                Value = Convert.ToHexString(MD5.HashData(hashSource)).ToLowerInvariant()
            },
            new ImageHash
            {
                Type = HashType.Sha1,
                Value = Convert.ToHexString(SHA1.HashData(hashSource)).ToLowerInvariant()
            }
        ];
    }

    /// <summary>
    /// Initializes a new image from a set of pre-computed hashes.
    /// </summary>
    /// <param name="hashes">The hashes of the image.</param>
    protected AthenaImageBase(IEnumerable<ImageHash> hashes)
    {
        ArgumentNullException.ThrowIfNull(hashes);
        _hashes = [.. hashes];
    }

    /// <summary>
    /// Gets the format of the image.
    /// </summary>
    public abstract ImageFormat Format { get; }

    /// <summary>
    /// Gets the hashes of the image. Hashes are computed from the original
    /// image data when the image is created.
    /// </summary>
    public IReadOnlyList<ImageHash> Hashes => _hashes;

    /// <summary>
    /// Gets a value indicating whether the hashes were derived from the image
    /// data. Hashes supplied directly by the caller are always sent to the
    /// API, whereas derived hashes are only sent when enabled in the client
    /// configuration.
    /// </summary>
    public virtual bool HasDerivedHashes => true;

    /// <summary>
    /// Gets the raw byte representation of the image.
    /// </summary>
    /// <returns>A span of bytes representing the image data.</returns>
    public abstract Span<byte> GetBytes();

    /// <summary>
    /// Gets the MD5 hash of the original image data, if available.
    /// </summary>
    /// <returns>A lowercase hexadecimal string representing the MD5 hash.</returns>
    public string? GetMd5Hash() => GetHash(HashType.Md5);

    /// <summary>
    /// Gets the SHA-1 hash of the original image data, if available.
    /// </summary>
    /// <returns>A lowercase hexadecimal string representing the SHA-1 hash.</returns>
    public string? GetSha1Hash() => GetHash(HashType.Sha1);

    private string? GetHash(HashType type) =>
        _hashes.FirstOrDefault(hash => hash.Type == type)?.Value;
}
