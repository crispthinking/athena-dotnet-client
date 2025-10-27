using Google.Protobuf;
using Microsoft.Extensions.Options;
using Resolver.Athena.Grpc;
using Resolver.AthenaApiClient;
using Resolver.AthenaClient.Images;
using Resolver.AthenaClient.Interfaces;

namespace Resolver.AthenaClient.Factories;

/// <summary>
/// Builds classifier inputs from Athena images.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AthenaClassificationInputFactory"/> class.
/// </remarks>
public sealed class AthenaClassificationInputFactory(IOptions<AthenaApiClientConfiguration> options) : IAthenaClassificationInputFactory
{
    private readonly AthenaApiClientConfiguration _configuration = options?.Value ?? throw new ArgumentNullException(nameof(options));

    /// <inheritdoc />
    public ClassificationInput Create(AthenaImageBase image, string? correlationId = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        var input = new ClassificationInput
        {
            Affiliate = _configuration.Affiliate,
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N"),
            Data = ByteString.CopyFrom(image.GetBytes()),
            Format = image.Format
        };

        if (_configuration.SendMd5Hash)
        {
            input.Hashes.Add(new ImageHash
            {
                Type = HashType.Md5,
                Value = image.ComputeMd5Hash()
            });
        }

        if (_configuration.SendSha1Hash)
        {
            input.Hashes.Add(new ImageHash
            {
                Type = HashType.Sha1,
                Value = image.ComputeSha1Hash()
            });
        }

        return input;
    }
}
