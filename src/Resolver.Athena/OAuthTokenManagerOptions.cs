namespace Resolver.Athena;

public record OAuthTokenManagerConfiguration
{
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }

    public string AuthUrl { get; set; } = "https://crispthinking.auth0.com/oauth/token";
    public string Audience { get; set; } = "crisp-athena-live";
}
