using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using Resolver.Athena.Grpc;
using Resolver.Athena.Images;
using Resolver.Athena.Interfaces;
using Resolver.Athena.Models;

namespace Resolver.Athena;

/// <summary>
/// Client for interacting with the Athena image classification service.
///
/// This client does not handle streaming requests. It provides methods for the
/// 'synchronous' methods of the Athena service, such as classifying a single image
/// or listing deployments.
/// </summary>
public class AthenaClient : IAthenaClient
{
    private readonly ClassifierService.ClassifierServiceClient _client;
    private readonly string _affiliate;
    private readonly bool _sendMd5Hash;
    private readonly bool _sendSha1Hash;

    public AthenaClient(ITokenManager tokenManager, IOptions<AthenaClientConfiguration> options, IAthenaClassifierServiceClientFactory athenaClassifierServiceClientFactory)
    {
        var channelOpts = new GrpcChannelOptions
        {
            Credentials = ChannelCredentials.Create(new SslCredentials(), CallCredentials.FromInterceptor(async (context, metadata) =>
            {
                var token = await tokenManager.GetTokenAsync(context.CancellationToken);
                metadata.Add("Authorization", $"Bearer {token}");
            }))
        };

        _client = athenaClassifierServiceClientFactory.Create(options.Value.Endpoint, channelOpts);
        _affiliate = options.Value.Affiliate;
        _sendMd5Hash = options.Value.SendMd5Hash;
        _sendSha1Hash = options.Value.SendSha1Hash;
    }

    public async Task<ClassificationResult> ClassifySingleImageAsync(AthenaImageBase imageData, CancellationToken cancellationToken)
    {
        var request = PrepareInput(imageData);

        var response = await _client.ClassifySingleAsync(request, cancellationToken: cancellationToken);

        return ClassificationResult.FromSingleOutput(response);
    }

    public async Task<List<DeploymentSummary>> ListDeploymentsAsync(CancellationToken cancellationToken)
    {
        var response = await _client.ListDeploymentsAsync(new(), cancellationToken: cancellationToken);
        return [.. response.Deployments.Select(deployment => new DeploymentSummary(deployment))];
    }

    protected ClassificationInput PrepareInput(AthenaImageBase imageData)
        => imageData.ToClassificationInput(_affiliate, _sendMd5Hash, _sendSha1Hash);
}
