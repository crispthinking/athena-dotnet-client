using Microsoft.Extensions.Options;
using Moq;
using Resolver.Athena.Client.ApiClient;
using Resolver.Athena.Client.ApiClient.Interfaces;
using Resolver.Athena.Client.HighLevelClient;
using Resolver.Athena.Client.HighLevelClient.Factories;
using Resolver.Athena.Client.HighLevelClient.Interfaces;
using Resolver.Athena.Client.HighLevelClient.Options;
using Resolver.Athena.Grpc;

namespace Resolver.Athena.Tests.Client;

public class AthenaClientTestsBase
{
    protected AthenaClient _athenaClient;
    protected Mock<IAthenaApiClient> _mockLowLevelClient;
    protected IOptions<AthenaApiClientConfiguration> _clientOptions;
    protected IAthenaClassificationInputFactory _inputFactory;
    protected IOptions<AthenaClientOptions> _streamingOptions;

    public AthenaClientTestsBase()
    {
        _mockLowLevelClient = new Mock<IAthenaApiClient>();

        _clientOptions = new OptionsWrapper<AthenaApiClientConfiguration>(new AthenaApiClientConfiguration
        {
            Endpoint = "https://mock-endpoint",
            Affiliate = "test-affiliate",
            SendMd5Hash = true,
            SendSha1Hash = true,
        });
        _inputFactory = new AthenaClassificationInputFactory(_clientOptions);
        _streamingOptions = new OptionsWrapper<AthenaClientOptions>(new AthenaClientOptions());
        _athenaClient = new AthenaClient(_mockLowLevelClient.Object, _inputFactory, _streamingOptions);
    }

    public AthenaClient GetAthenaClient(AthenaApiClientConfiguration config)
    {
        var options = new OptionsWrapper<AthenaApiClientConfiguration>(config);
        var inputFactory = new AthenaClassificationInputFactory(options);
        return new AthenaClient(_mockLowLevelClient.Object, inputFactory, _streamingOptions);
    }
}
