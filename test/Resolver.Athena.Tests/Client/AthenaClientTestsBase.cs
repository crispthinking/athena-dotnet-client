using Microsoft.Extensions.Options;
using Moq;
using Resolver.Athena.Grpc;
using Resolver.AthenaApiClient;
using Resolver.AthenaApiClient.Interfaces;
using Resolver.AthenaClient.Factories;
using Resolver.AthenaClient.Interfaces;
using Resolver.AthenaClient.Models;
using Resolver.AthenaClient.Options;

namespace Resolver.Athena.Tests.Client;

public class AthenaClientTestsBase
{
    protected Resolver.AthenaClient.AthenaClient _athenaClient;
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
        _athenaClient = new Resolver.AthenaClient.AthenaClient(_mockLowLevelClient.Object, _inputFactory, _streamingOptions);
    }

    public Resolver.AthenaClient.AthenaClient GetAthenaClient(AthenaApiClientConfiguration config)
    {
        var options = new OptionsWrapper<AthenaApiClientConfiguration>(config);
        var inputFactory = new AthenaClassificationInputFactory(options);
        return new Resolver.AthenaClient.AthenaClient(_mockLowLevelClient.Object, inputFactory, _streamingOptions);
    }
}
