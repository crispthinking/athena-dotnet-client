using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Resolver.Athena.Tests.TestSupport;

/// <summary>
/// A wrapper around <see cref="OAuthTokenManager"/> that exposes internal state for testing.
/// </summary>
public class TestOAuthTokenManager(IOptions<OAuthTokenManagerConfiguration> options, IHttpClientFactory httpClientFactory, ILogger<OAuthTokenManager> logger) : OAuthTokenManager(options, httpClientFactory, logger)
{
    public string? GetToken()
        => _token;

    public DateTime GetTokenExpiry()
        => _tokenExpiry;

    public void SetToken(string? token)
        => _token = token;

    public void SetTokenExpiry(DateTime expiry)
        => _tokenExpiry = expiry;
}
