namespace Resolver.Athena.Client.ApiClient.Interfaces;

/// <summary>
/// Contract for managing OAuth tokens.
/// </summary>
public interface ITokenManager
{
    /// <summary>
    /// Gets an access token for the specified scope.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The access token.</returns>
    Task<string> GetTokenAsync(CancellationToken cancellationToken);
}
