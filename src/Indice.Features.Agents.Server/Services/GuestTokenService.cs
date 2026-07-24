using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Server.Services;

/// <summary>Default <see cref="IGuestTokenService"/> that mints guest tokens through the <c>urn:indice:guest</c> extension grant using client credentials.</summary>
internal class GuestTokenService : IGuestTokenService
{
    private readonly HttpClient _httpClient;
    private readonly AgentsServerOptions _options;

    /// <summary>Creates a new instance of <see cref="GuestTokenService"/>.</summary>
    public GuestTokenService(HttpClient httpClient, IOptions<AgentsServerOptions> options) {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<GuestAccessToken> CreateTokenAsync(string? authorName = null, CancellationToken cancellationToken = default) {
        var guestTokenOptions = _options.GuestToken;
        var tokenEndpoint = guestTokenOptions.TokenEndpoint;
        if (string.IsNullOrWhiteSpace(tokenEndpoint)) {
            var authority = guestTokenOptions.Authority?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(authority)) {
                throw new InvalidOperationException($"{nameof(GuestTokenOptions)}.{nameof(GuestTokenOptions.Authority)} or {nameof(GuestTokenOptions.TokenEndpoint)} must be configured.");
            }
            tokenEndpoint = $"{authority}/connect/token";
        }
        var form = new Dictionary<string, string> {
            ["grant_type"] = guestTokenOptions.GrantType,
            ["scope"] = guestTokenOptions.Scope ?? _options.ChatRequiredScope,
            ["client_id"] = guestTokenOptions.ClientId ?? throw new InvalidOperationException($"{nameof(GuestTokenOptions)}.{nameof(GuestTokenOptions.ClientId)} is not configured."),
            ["client_secret"] = guestTokenOptions.ClientSecret ?? throw new InvalidOperationException($"{nameof(GuestTokenOptions)}.{nameof(GuestTokenOptions.ClientSecret)} is not configured."),
        };
        if (!string.IsNullOrWhiteSpace(authorName)) {
            form.Add("given_name", authorName);
        }
        using var requestContent = new FormUrlEncodedContent(form);
        using var response = await _httpClient.PostAsync(tokenEndpoint, requestContent, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) {
            throw new InvalidOperationException($"Guest token request failed with status {(int)response.StatusCode}: {payload}");
        }
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var accessToken = root.GetProperty("access_token").GetString()!;
        var tokenType = root.TryGetProperty("token_type", out var tokenTypeElement) ? tokenTypeElement.GetString() ?? "Bearer" : "Bearer";
        var expiresIn = root.TryGetProperty("expires_in", out var expiresInElement) ? expiresInElement.GetInt32() : 0;
        var subject = root.TryGetProperty("sub", out var subjectElement)
            ? subjectElement.GetString()!
            : throw new InvalidOperationException("Guest token response did not contain the 'sub' custom field. Ensure the identity provider registers the guest grant validator.");
        return new GuestAccessToken(accessToken, tokenType, expiresIn, subject);
    }
}
