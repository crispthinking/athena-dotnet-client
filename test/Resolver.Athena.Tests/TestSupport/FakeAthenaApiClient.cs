using System.Threading.Channels;
using Resolver.Athena.Grpc;
using Resolver.AthenaApiClient.Interfaces;

namespace Resolver.Athena.Tests.TestSupport;

/// <summary>
/// Fake implementation of <see cref="IAthenaApiClient"/> for testing purposes.
/// </summary>
public class FakeAthenaApiClient(Queue<ClassifyResponse> providedResponses) : IAthenaApiClient
{
    private readonly Queue<ClassifyResponse> _providedResponses = providedResponses;
    private readonly List<ClassifyRequest> _receivedRequests = [];

    public IReadOnlyList<ClassifyRequest> ReceivedRequests => _receivedRequests;

    public Task<Channel<ClassifyResponse>> ClassifyAsync(ChannelReader<ClassifyRequest> requestChannel, int responseChannelCapacity, CancellationToken cancellationToken)
    {
        var responseChannel = Channel.CreateBounded<ClassifyResponse>(new BoundedChannelOptions(responseChannelCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

        _ = Task.Run(async () =>
        {
            await foreach (var request in requestChannel.ReadAllAsync(cancellationToken))
            {
                _receivedRequests.Add(request);
                if (_providedResponses.Count == 0)
                {
                    continue;
                }
                var response = _providedResponses.Dequeue();
                for (var i = 0; i < response.Outputs.Count; i++)
                {
                    // Correlate response outputs with request inputs if possible
                    if (i < request.Inputs.Count)
                    {
                        response.Outputs[i].CorrelationId = request.Inputs[i].CorrelationId;
                    }
                    else
                    {
                        // provide a dummy correlation id, since there is no
                        // matching input - means batches are differntly sized
                        // on input vs output. This only happens in the testing
                        // since we arent actually processing inputs and
                        // assigning real correlation ids.
                        response.Outputs[i].CorrelationId = "test-correlation-id";
                    }
                }
                await responseChannel.Writer.WriteAsync(response, cancellationToken);
            }

            responseChannel.Writer.Complete();
        }, cancellationToken);


        return Task.FromResult(responseChannel);
    }

    public Task<ClassificationOutput> ClassifySingleAsync(ClassificationInput input, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<ListDeploymentsResponse> ListDeploymentsAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Builder for <see cref="FakeAthenaApiClient"/>.
    /// </summary>
    public class FakeAthenaApiClientBuilder
    {
        private readonly Queue<ClassifyResponse> _providedResponses = new();

        /// <summary>
        /// Adds a response to be returned by the fake client. This response
        /// will be added to the back of the queue.
        /// </summary>
        /// <param name="response">The response to add.</param>
        /// <returns>The builder instance.</returns>
        public FakeAthenaApiClientBuilder WithQueuedResponse(ClassifyResponse response)
        {
            _providedResponses.Enqueue(response);
            return this;
        }

        /// <summary>
        /// Adds a response to be returned by the fake client. This response
        /// will be added to the back of the queue.
        /// </summary>
        /// <param name="responseValues">The classification labels and weights
        /// for the response. Each pair is a single classification, contained
        /// within a list that makes up a single output. This method takes a
        /// number of lists, which will be multiple outputs collected into one
        /// response, to simulate batching.</param>
        /// <returns>The builder instance.</returns>
        public FakeAthenaApiClientBuilder WithQueuedResponse(params List<(string, float)>[] responseValues)
        {
            var response = new ClassifyResponse();
            foreach (var classificationOutput in responseValues)
            {
                var output = new ClassificationOutput();
                foreach ((var label, var weight) in classificationOutput)
                {
                    output.Classifications.Add(new Classification
                    {
                        Label = label,
                        Weight = weight
                    });
                }

                response.Outputs.Add(output);
            }

            return WithQueuedResponse(response);
        }

        /// <summary>
        /// Adds an error response to be returned by the fake client. This response
        /// will be added to the back of the queue.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The builder instance.</returns>
        public FakeAthenaApiClientBuilder WithQueuedErrorResponse(string message)
        {
            var resp = new ClassifyResponse();
            resp.Outputs.Add(new ClassificationOutput
            {
                Error = new()
                {
                    Code = ErrorCode.Unspecified,
                    Message = message
                }
            });
            return WithQueuedResponse(resp);
        }

        public FakeAthenaApiClient Build()
        {
            return new FakeAthenaApiClient(_providedResponses);
        }
    }
}
