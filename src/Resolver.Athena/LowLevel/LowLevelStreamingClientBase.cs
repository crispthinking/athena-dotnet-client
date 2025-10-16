using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using Resolver.Athena.Grpc;
using Resolver.Athena.Interfaces;

namespace Resolver.Athena.LowLevel;

public class LowLevelStreamingClientBase
{
    protected ClassifierService.ClassifierServiceClient Client { get; }

    protected IClientStreamWriter<ClassifyRequest>? RequestStream { get; set; }
    protected IAsyncStreamReader<ClassifyResponse>? ResponseStream { get; set; }

    protected string DeploymentId { get; init; }

    public LowLevelStreamingClientBase(ITokenManager tokenManager, IOptions<LowLevelStreamingConfiguration> options, IAthenaClassifierServiceClientFactory athenaClassifierServiceClientFactory)
    {
        var channelOpts = new GrpcChannelOptions
        {
            Credentials = ChannelCredentials.Create(new SslCredentials(), CallCredentials.FromInterceptor(async (context, metadata) =>
            {
                var token = await tokenManager.GetTokenAsync(context.CancellationToken);
                metadata.Add("Authorization", $"Bearer {token}");
            }))
        };

        Client = athenaClassifierServiceClientFactory.Create(options.Value.Endpoint, channelOpts);
        DeploymentId = options.Value.DeploymentId;
    }

    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        var streams = Client.Classify(cancellationToken: cancellationToken);
        RequestStream = streams.RequestStream;
        ResponseStream = streams.ResponseStream;

        return Task.CompletedTask;
    }

    public virtual async Task StopAsync(CancellationToken cancellationToken)
    {
        if (RequestStream == null && ResponseStream == null)
        {
            return;
        }

        if (RequestStream != null)
        {
            await RequestStream.CompleteAsync();
        }

        RequestStream = null;
        ResponseStream = null;
    }

    /// <summary>
    /// Returns an async enumerable of responses from the gRPC stream.
    /// </summary>
    public IAsyncEnumerable<ClassifyResponse> GetResponsesAsync(CancellationToken cancellationToken)
    {
        if (ResponseStream == null)
        {
            throw new InvalidOperationException($"Stream has not been started. Call {nameof(StartAsync)} first.");
        }

        return ResponseStream.ReadAllAsync(cancellationToken);
    }

    protected Task SendAsync(ClassifyRequest request, CancellationToken cancellationToken)
    {
        if (RequestStream == null)
        {
            throw new InvalidOperationException($"Stream has not been started. Call {nameof(StartAsync)} first.");
        }

        return RequestStream.WriteAsync(request, cancellationToken);
    }
}
