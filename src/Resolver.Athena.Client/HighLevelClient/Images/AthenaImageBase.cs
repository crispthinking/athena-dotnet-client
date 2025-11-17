using System.Security.Cryptography;
using Resolver.Athena.Grpc;

namespace Resolver.Athena.Client.HighLevelClient.Images;

/// <summary>
/// Base class for Athena image representations.
/// </summary>
public abstract class AthenaImageBase
{
    /// <summary>
    /// Gets the format of the image.
    /// </summary>
    public abstract ImageFormat Format { get; }

    /// <summary>
    /// Gets the raw byte representation of the image.
    /// </summary>
    /// <returns>A span of bytes representing the image data.</returns>
    public abstract Span<byte> GetBytes();

    /// <summary>
    /// Computes the MD5 hash of the image data.
    /// </summary>
    /// <returns>A lowercase hexadecimal string representing the MD5 hash.</returns>
    public virtual string ComputeMd5Hash()
    {
        return Convert.ToHexString(MD5.HashData(GetBytes())).ToLowerInvariant();
    }

    /// <summary>
    /// Computes the SHA-1 hash of the image data.
    /// </summary>
    /// <returns>A lowercase hexadecimal string representing the SHA-1 hash.</returns>
    public virtual string ComputeSha1Hash()
    {
        return Convert.ToHexString(SHA1.HashData(GetBytes())).ToLowerInvariant();
    }
}
