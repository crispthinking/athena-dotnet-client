namespace Resolver.Athena.Auth;

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resolver.Athena.Interfaces;

/// <summary>
/// Manages OAuth tokens for authenticating with the Athena service.
/// </summary>
public class OAuthTokenManager(IOptions<OAuthTokenManagerConfiguration> options, IHttpClientFactory httpClientFactory, ILogger<OAuthTokenManager> logger) : ITokenManager
{
    private readonly string _clientId = options.Value.ClientId;
    private readonly string _clientSecret = options.Value.ClientSecret;
    private readonly string _authUrl = options.Value.AuthUrl;
    private readonly string _audience = options.Value.Audience;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
    private readonly ILogger<OAuthTokenManager> _logger = logger;

    protected string? _token;
    protected DateTime _tokenExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Retrieve a valid cached token, or generate a new one and cache it.
    /// </summary>
    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_token == null || DateTime.UtcNow >= _tokenExpiry)
            {
                _logger.LogDebug("Access token is missing or expired, refreshing token.");
                await RefreshTokenAsync(cancellationToken);
            }
            return _token ?? throw new InvalidOperationException("Could not obtain access token. Ensure OAuth server is reachable and credentials are correct.");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task RefreshTokenAsync(CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            client_id = _clientId,
            client_secret = _clientSecret,
            audience = _audience,
            grant_type = "client_credentials"
        };

        _logger.LogDebug("Requesting new access token from {AuthUrl} for audience {Audience}", _authUrl, _audience);
        var response = await _httpClient.PostAsJsonAsync(_authUrl, requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();
        _logger.LogDebug("Access token response received with status code {StatusCode}", response.StatusCode);

        var responseBody = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Failed to parse token response.");
        _token = responseBody.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(responseBody.ExpiresIn - 60); // Refresh 1 minute before expiry
        _logger.LogInformation("Obtained new access token, expires at {Expiry}", _tokenExpiry);
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public required string AccessToken { get; set; }

        [JsonPropertyName("scope")]
        public required string Scope { get; set; }

        [JsonPropertyName("expires_in")]
        public required int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public required string TokenType { get; set; }
    }
}
