using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using Resolver.Athena.Grpc;
using Resolver.Athena.Interfaces;

namespace Resolver.Athena;

public class AthenaClientFactory(IOptions<AthenaClientFactoryConfiguration> options, ITokenManager tokenManager) : IAthenaClientFactory
{
    private readonly ITokenManager _tokenManager = tokenManager;
    private readonly string _endpoint = options.Value.Endpoint;

    public IAthenaClient Create()
    {
        var channelOpts = new GrpcChannelOptions
        {
            Credentials = ChannelCredentials.Create(new SslCredentials(), CallCredentials.FromInterceptor(async (context, metadata) =>
            {
                var token = await _tokenManager.GetTokenAsync(context.CancellationToken);
                metadata.Add("Authorization", $"Bearer {token}");
            }))
        };
        var channel = GrpcChannel.ForAddress(_endpoint, channelOpts);
        var client = new ClassifierService.ClassifierServiceClient(channel);
        return new AthenaClient(client);
    }
}
