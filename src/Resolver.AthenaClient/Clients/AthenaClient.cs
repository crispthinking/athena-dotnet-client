using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Resolver.Athena.Grpc;
using Resolver.AthenaApiClient.Interfaces;
using Resolver.AthenaClient.Images;
using Resolver.AthenaClient.Interfaces;
using Resolver.AthenaClient.Models;
using Resolver.AthenaClient.Options;

namespace Resolver.AthenaClient;

/// <summary>
/// High-level client for interacting with the Athena API.
/// </summary>
public class AthenaClient(
    IAthenaApiClient apiClient,
    IAthenaClassificationInputFactory inputFactory,
    IOptions<AthenaClientOptions> streamingOptions) : IAthenaClient
{
    private readonly IAthenaApiClient _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    private readonly IAthenaClassificationInputFactory _inputFactory = inputFactory ?? throw new ArgumentNullException(nameof(inputFactory));
    private readonly IOptions<AthenaClientOptions> _streamingOptions = streamingOptions ?? throw new ArgumentNullException(nameof(streamingOptions));

    /// <inheritdoc />
    public async Task<ClassificationResult> ClassifySingleAsync(AthenaImageBase imageData, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(imageData);
        var request = _inputFactory.Create(imageData);
        var response = await _apiClient.ClassifySingleAsync(request, cancellationToken).ConfigureAwait(false);
        return ClassificationResult.FromSingleOutput(response);
    }

    /// <inheritdoc />
    public async Task<List<DeploymentSummary>> ListDeploymentsAsync(CancellationToken cancellationToken)
    {
        var response = await _apiClient.ListDeploymentsAsync(cancellationToken).ConfigureAwait(false);
        return [.. response.Deployments.Select(deployment => new DeploymentSummary(deployment))];
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ClassificationResult> ClassifyAsync(
        IAsyncEnumerable<ClassificationRequest> requests,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requests);
        var options = _streamingOptions.Value;
        var channelOptions = new BoundedChannelOptions(options.ResponseChannelCapacity)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        };
        var channel = Channel.CreateBounded<ClassificationResult>(channelOptions);

        var session = await _apiClient.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
        var disposed = 0;

        async Task DisposeSessionOnceAsync()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 1)
            {
                return;
            }

            await session.DisposeAsync().ConfigureAwait(false);
        }

        var sendTask = SendRequestsAsync(session, channel.Writer, requests, options, DisposeSessionOnceAsync, cancellationToken);
        var pumpTask = PumpResponsesAsync(session, channel.Writer, cancellationToken);

        Exception? sendException = null;
        Exception? sendFault = null;
        Exception? pumpFault = null;
        try
        {
            await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return result;
            }
        }
        finally
        {
            try
            {
                sendException = await sendTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                sendFault = ex;
            }

            try
            {
                await pumpTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                pumpFault = ex;
            }

            await DisposeSessionOnceAsync().ConfigureAwait(false);
        }

        if (sendFault is not null)
        {
            throw sendFault;
        }

        if (pumpFault is not null)
        {
            throw pumpFault;
        }

        if (sendException is not null)
        {
            throw sendException;
        }
    }

    private static bool CaptureSendException(
        Exception exception,
        CancellationToken cancellationToken,
        out Exception? captured)
    {
        captured = exception;
        if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
        {
            captured = new OperationCanceledException("Streaming was cancelled before completing all requests.", exception, cancellationToken);
            return true;
        }

        if (exception is RpcException or InvalidOperationException)
        {
            return true;
        }

        return false;
    }

    private async Task<Exception?> SendRequestsAsync(
        IAthenaSession session,
        ChannelWriter<ClassificationResult> writer,
        IAsyncEnumerable<ClassificationRequest> requests,
        AthenaClientOptions options,
        Func<Task> disposeSessionAsync,
        CancellationToken cancellationToken)
    {
        Exception? sendException = null;

        try
        {
            await foreach (var request in requests.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                var correlationId = request.CorrelationId ?? options.CorrelationIdFactory();
                var input = _inputFactory.Create(request.Image, correlationId);

                var classifyRequest = new ClassifyRequest
                {
                    DeploymentId = request.DeploymentId
                };

                classifyRequest.Inputs.Add(input);
                await session.SendAsync(classifyRequest, cancellationToken).ConfigureAwait(false);
            }

            await session.CompleteAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (CaptureSendException(ex, cancellationToken, out sendException))
        {
            writer.TryComplete(sendException);
            await disposeSessionAsync().ConfigureAwait(false);
        }

        return sendException;
    }

    private static async Task PumpResponsesAsync(
        IAthenaSession session,
        ChannelWriter<ClassificationResult> writer,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var response in session.ReadResponsesAsync(cancellationToken).ConfigureAwait(false))
            {
                if (response.GlobalError is not null)
                {
                    var result = ClassificationResult.FromGlobalError(response.GlobalError);
                    await writer.WriteAsync(result, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                foreach (var output in response.Outputs)
                {
                    var result = ClassificationResult.FromSingleOutput(output);
                    await writer.WriteAsync(result, cancellationToken).ConfigureAwait(false);
                }
            }

            writer.TryComplete();
        }
        catch (Exception ex)
        {
            writer.TryComplete(ex);
        }
    }
}
