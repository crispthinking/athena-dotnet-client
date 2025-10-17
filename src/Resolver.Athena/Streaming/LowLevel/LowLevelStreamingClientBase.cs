using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using Resolver.Athena.Grpc;
using Resolver.Athena.Images;
using Resolver.Athena.Interfaces;

namespace Resolver.Athena.Streaming.LowLevel;

public class LowLevelStreamingClientBase : ILowLevelStreamingClient
{
    /// <summary>
    /// The gRPC client for the Athena Classifier Service.
    /// </summary>
    protected ClassifierService.ClassifierServiceClient Client { get; }

    protected IClientStreamWriter<ClassifyRequest>? RequestStream { get; set; }
    protected IAsyncStreamReader<ClassifyResponse>? ResponseStream { get; set; }

    protected string Affiliate { get; init; }
    protected string DeploymentId { get; init; }

    private readonly bool _sendMd5Hash;
    private readonly bool _sendSha1Hash;

    public LowLevelStreamingClientBase(ITokenManager tokenManager, IOptions<LowLevelStreamingClientConfiguration> options, IAthenaClassifierServiceClientFactory athenaClassifierServiceClientFactory)
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
        Affiliate = options.Value.Affiliate;

        _sendMd5Hash = options.Value.SendMd5Hash;
        _sendSha1Hash = options.Value.SendSha1Hash;
    }

    /// <summary>
    /// Starts the gRPC streaming call and initializes the request and response streams.
    ///
    /// This method must be called before sending requests or receiving responses.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the stream has already been started.</exception>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        if (RequestStream != null || ResponseStream != null)
        {
            throw new InvalidOperationException($"Stream has already been started. Call {nameof(StopAsync)} before starting again.");
        }

        var streams = Client.Classify(cancellationToken: cancellationToken);
        RequestStream = streams.RequestStream;
        ResponseStream = streams.ResponseStream;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the gRPC streaming call and cleans up the request and response streams.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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
    /// <exception cref="InvalidOperationException">Thrown if the stream has not been started.</exception>
    public IAsyncEnumerable<ClassifyResponse> GetResponsesAsync(CancellationToken cancellationToken)
    {
        if (ResponseStream == null)
        {
            throw new InvalidOperationException($"Stream has not been started. Call {nameof(StartAsync)} first.");
        }

        return ResponseStream.ReadAllAsync(cancellationToken);
    }

    /// <summary>
    /// Prepares a ClassificationInput message from the given AthenaImageBase.
    /// </summary>
    public virtual ClassificationInput PrepareInput(AthenaImageBase athenaImageBase)
        => athenaImageBase.ToClassificationInput(Affiliate, _sendMd5Hash, _sendSha1Hash);

    /// <summary>
    /// Wraps the sending of a ClassifyRequest to the gRPC stream, ensuring
    /// the stream has been started.
    /// </summary>
    protected Task SendAsync(ClassifyRequest request, CancellationToken cancellationToken)
    {
        if (RequestStream == null)
        {
            throw new InvalidOperationException($"Stream has not been started. Call {nameof(StartAsync)} first.");
        }

        return RequestStream.WriteAsync(request, cancellationToken);
    }
}
