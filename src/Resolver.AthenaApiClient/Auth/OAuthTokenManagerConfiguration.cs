namespace Resolver.AthenaApiClient.Auth;

/// <summary>
/// Configuration options for OAuth token management.
/// </summary>
public sealed record OAuthTokenManagerConfiguration
{
    /// <summary>
    /// The OAuth client ID.
    /// </summary>
    public required string ClientId { get; set; }

    /// <summary>
    /// The OAuth client secret.
    /// </summary>
    public required string ClientSecret { get; set; }

    /// <summary>
    /// The authentication URL.
    /// </summary>
    public string AuthUrl { get; set; } = "https://crispthinking.auth0.com/oauth/token";

    /// <summary>
    /// The audience for the token.
    /// </summary>
    public string Audience { get; set; } = "crisp-athena-live";
}
