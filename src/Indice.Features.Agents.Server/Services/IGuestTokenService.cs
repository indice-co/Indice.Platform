namespace Indice.Features.Agents.Server.Services;

/// <summary>Acquires ephemeral guest access tokens from the identity provider through the backchannel (<c>/connect/token</c>).</summary>
public interface IGuestTokenService
{
    /// <summary>Requests a new guest access token. The identity provider generates a fresh guest subject and echoes it back.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<GuestAccessToken> CreateTokenAsync(string? authorName = null, CancellationToken cancellationToken = default);
}

/// <summary>An ephemeral guest access token issued by the identity provider.</summary>
/// <param name="AccessToken">The access token to use as a bearer token on subsequent calls.</param>
/// <param name="TokenType">The token type (typically <c>Bearer</c>).</param>
/// <param name="ExpiresIn">Token lifetime in seconds.</param>
/// <param name="Subject">The guest subject identifier the token was issued for.</param>
/// <param name="RefreshToken">The refresh token</param>
public record GuestAccessToken(string AccessToken, string TokenType, int ExpiresIn, string Subject, string? RefreshToken);
