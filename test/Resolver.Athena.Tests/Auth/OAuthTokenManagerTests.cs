using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Resolver.Athena.Client.ApiClient.Auth;
using Resolver.Athena.Tests.TestSupport;
using RichardSzalay.MockHttp;

namespace Resolver.Athena.Tests.Auth;

public class OAuthTokenManagerTests
{
    private readonly Mock<IOptions<OAuthTokenManagerConfiguration>> _optionsMock;
    private readonly MockHttpMessageHandler _mockHttpMessageHandler = new();
    private readonly TestOAuthTokenManager _tokenManager;

    public OAuthTokenManagerTests()
    {
        _optionsMock = new Mock<IOptions<OAuthTokenManagerConfiguration>>();

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(_mockHttpMessageHandler));

        var config = new OAuthTokenManagerConfiguration
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            AuthUrl = "https://example.com/oauth/token",
            Audience = "test-audience"
        };

        _optionsMock.Setup(o => o.Value).Returns(config);

        var nullLogger = new NullLogger<OAuthTokenManager>();

        _tokenManager = new TestOAuthTokenManager(_optionsMock.Object, httpClientFactoryMock.Object, nullLogger);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldRefreshToken_WhenTokenIsNull()
    {
        // Arrange
        _tokenManager.SetToken(null);
        _tokenManager.SetTokenExpiry(DateTime.MinValue);

        var tokenResponse = GenerateTokenResponse("new-access-token", 3600);
        var httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(tokenResponse, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.When("https://example.com/oauth/token")
            .Respond(req => httpResponse);

        // Act
        var token = await _tokenManager.GetTokenAsync(CancellationToken.None);

        // Assert
        Assert.Equal("new-access-token", token);
        Assert.NotNull(_tokenManager.GetToken());
        Assert.True(_tokenManager.GetTokenExpiry() > DateTime.UtcNow);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldNotRefreshToken_WhenTokenValid()
    {
        // Arrange
        var expectedToken = "old-token";
        _tokenManager.SetToken(expectedToken);

        var expectedExpiry = DateTime.UtcNow + TimeSpan.FromMinutes(10);
        _tokenManager.SetTokenExpiry(expectedExpiry);

        var tokenResponse = GenerateTokenResponse("new-access-token", 3600);
        var httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(tokenResponse, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.When("https://example.com/oauth/token")
            .Respond(req => httpResponse);

        // Act
        var token = await _tokenManager.GetTokenAsync(CancellationToken.None);

        // Assert
        Assert.Equal(expectedToken, token);
        Assert.NotNull(_tokenManager.GetToken());
        Assert.True(_tokenManager.GetTokenExpiry() > DateTime.UtcNow);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldRefreshToken_WhenTokenExpired()
    {
        // Arrange
        _tokenManager.SetToken("old-token");

        _tokenManager.SetTokenExpiry(DateTime.UtcNow - TimeSpan.FromMinutes(10));

        var tokenResponse = GenerateTokenResponse("new-token", 3600);
        var httpResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(tokenResponse, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.When("https://example.com/oauth/token")
            .Respond(req => httpResponse);

        // Act
        var token = await _tokenManager.GetTokenAsync(CancellationToken.None);

        // Assert
        Assert.Equal("new-token", token);
        Assert.NotNull(_tokenManager.GetToken());
        Assert.True(_tokenManager.GetTokenExpiry() > DateTime.UtcNow);
    }

    private static string GenerateTokenResponse(string accessToken, int expiresIn)
    {
        return $$"""
            {
                "access_token": "{{accessToken}}",
                "expires_in": {{expiresIn}},
                "token_type": "Bearer",
                "scope": "test-scope"
            }
            """;
    }
}
