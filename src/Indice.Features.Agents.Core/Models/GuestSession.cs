using System.Text.Json.Serialization;

namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// Ephemeral guest credentials issued when a chat session is created anonymously.
/// Present only on the response of an anonymous create; the client must use <see cref="AccessToken"/> as a bearer token on all subsequent calls.
/// </summary>
public class GuestSession
{
    /// <summary>The access token to send as a bearer token on subsequent calls.</summary>
    public required string AccessToken { get; set; }

    /// <summary>The token type (typically <c>Bearer</c>).</summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>Token lifetime in seconds.</summary>
    public int ExpiresIn { get; set; }

    /// <summary>The guest subject identifier the token was issued for.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Subject { get; set; }

    /// <summary>Optionally the refresh token to renew this session. </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RefreshToken { get; set; }
}
