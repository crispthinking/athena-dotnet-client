using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Options;
using Resolver.Athena.Grpc;
using Resolver.AthenaApiClient.Interfaces;
using Resolver.AthenaClient.Interfaces;
using Resolver.AthenaClient.Models;
using Resolver.AthenaClient.Options;
using Resolver.AthenaDataflowClient.Interfaces;
using Resolver.AthenaDataflowClient.Models;
using Resolver.AthenaDataflowClient.Options;

namespace Resolver.AthenaDataflowClient.Clients;

/// <summary>
/// Athena TPL dataflow client.
/// </summary>
public sealed class AthenaDataflowClient(
    IAthenaApiClient apiClient,
    IAthenaClassificationInputFactory inputFactory,
    IOptions<AthenaClientOptions> streamingOptions,
    IOptions<AthenaDataflowClientOptions> dataflowOptions) : IAthenaDataflowClient
{
    private readonly IAthenaApiClient _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    private readonly IAthenaClassificationInputFactory _inputFactory = inputFactory ?? throw new ArgumentNullException(nameof(inputFactory));
    private readonly IOptions<AthenaClientOptions> _streamingOptions = streamingOptions ?? throw new ArgumentNullException(nameof(streamingOptions));
    private readonly IOptions<AthenaDataflowClientOptions> _dataflowOptions = dataflowOptions ?? throw new ArgumentNullException(nameof(dataflowOptions));

    /// <inheritdoc />
    public async Task<AthenaDataflowPipeline> CreatePipelineAsync(CancellationToken cancellationToken = default)
    {
        var streamingOptions = _streamingOptions.Value;
        var dataflowOptions = _dataflowOptions.Value;
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pipelineToken = linkedCts.Token;

        var requestChannel = Channel.CreateBounded<ClassifyRequest>(new BoundedChannelOptions(streamingOptions.RequestChannelCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

        var responseChannel = await _apiClient.ClassifyAsync(requestChannel.Reader, streamingOptions.ResponseChannelCapacity, pipelineToken).ConfigureAwait(false);
        var outputBuffer = new BufferBlock<ClassificationResult>(new DataflowBlockOptions
        {
            BoundedCapacity = dataflowOptions.ResponseBufferCapacity,
            CancellationToken = pipelineToken
        });

        var sendBlock = new ActionBlock<ClassificationRequest>(
            request => SendAsync(requestChannel.Writer, request, streamingOptions, pipelineToken),
            new ExecutionDataflowBlockOptions
            {
                CancellationToken = pipelineToken,
                BoundedCapacity = dataflowOptions.InputBufferCapacity,
                MaxDegreeOfParallelism = dataflowOptions.MaxWriteDegreeOfParallelism
            });

        var responsePump = PumpResponsesAsync(responseChannel.Reader, outputBuffer, pipelineToken);
        var completionTask = MonitorPipelineAsync(requestChannel.Writer, sendBlock, responsePump, outputBuffer, linkedCts);

        async ValueTask DisposeAsync()
        {
            linkedCts.Cancel();
            sendBlock.Complete();
            try
            {
                await completionTask.ConfigureAwait(false);
            }
            catch
            {
                // swallow to avoid surfacing disposal failures; completion reflects the real state.
            }
            finally
            {
                linkedCts.Dispose();
            }
        }

        return new AthenaDataflowPipeline(sendBlock, outputBuffer, completionTask, DisposeAsync);
    }

    private async Task SendAsync(ChannelWriter<ClassifyRequest> channel, ClassificationRequest request, AthenaClientOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var correlationId = request.CorrelationId ?? options.CorrelationIdFactory();
        var input = _inputFactory.Create(request.Image, correlationId);

        var classifyRequest = new ClassifyRequest
        {
            DeploymentId = request.DeploymentId
        };

        classifyRequest.Inputs.Add(input);
        await channel.WriteAsync(classifyRequest, cancellationToken).ConfigureAwait(false);
    }

    private static async Task PumpResponsesAsync(
        ChannelReader<ClassifyResponse> channel,
        BufferBlock<ClassificationResult> outputBuffer,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var response in channel.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                if (response.GlobalError is not null)
                {
                    var error = ClassificationResult.FromGlobalError(response.GlobalError);
                    await outputBuffer.SendAsync(error, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                foreach (var output in response.Outputs)
                {
                    var result = ClassificationResult.FromSingleOutput(output);
                    await outputBuffer.SendAsync(result, cancellationToken).ConfigureAwait(false);
                }
            }

            outputBuffer.Complete();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            outputBuffer.Complete();
            throw;
        }
        catch (Exception ex)
        {
            ((IDataflowBlock)outputBuffer).Fault(ex);
            throw;
        }
    }

    private static async Task MonitorPipelineAsync(
        ChannelWriter<ClassifyRequest> requestChannel,
        ActionBlock<ClassificationRequest> sendBlock,
        Task responsePump,
        BufferBlock<ClassificationResult> outputBuffer,
        CancellationTokenSource linkedCts)
    {
        Exception? failure = null;
        try
        {
            await sendBlock.Completion.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            failure = ex;
        }

        if (failure is null && !linkedCts.IsCancellationRequested)
        {
            try
            {
                requestChannel.TryComplete(failure);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        }

        try
        {
            await responsePump.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            failure ??= ex;
        }

        if (failure is not null)
        {
            if (failure is OperationCanceledException && linkedCts.IsCancellationRequested)
            {
                outputBuffer.Complete();
                throw new OperationCanceledException("Dataflow pipeline cancelled.", failure, linkedCts.Token);
            }

            ((IDataflowBlock)outputBuffer).Fault(failure);
            throw failure;
        }
    }
}
