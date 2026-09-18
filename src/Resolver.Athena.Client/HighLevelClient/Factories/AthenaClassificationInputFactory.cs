using Google.Protobuf;
using Microsoft.Extensions.Options;
using Resolver.Athena.Client.ApiClient;
using Resolver.Athena.Client.HighLevelClient.Images;
using Resolver.Athena.Client.HighLevelClient.Interfaces;
using Resolver.Athena.Grpc;

namespace Resolver.Athena.Client.HighLevelClient.Factories;

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

        foreach (var hash in image.Hashes)
        {
            if (image.HasDerivedHashes && !ShouldSend(hash.Type))
            {
                continue;
            }

            input.Hashes.Add(hash.Clone());
        }

        return input;
    }

    private bool ShouldSend(HashType type) => type switch
    {
        HashType.Md5 => _configuration.SendMd5Hash,
        HashType.Sha1 => _configuration.SendSha1Hash,
        _ => false
    };
}
