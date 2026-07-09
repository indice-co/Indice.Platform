using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

internal sealed class ClientCredentialsBearerHandler : DelegatingHandler
{
    private readonly string _tokenEndpoint;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _scope;

    private string? _token;
    private DateTimeOffset _expiry;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ClientCredentialsBearerHandler(
        string tokenEndpoint, string clientId, string clientSecret, string? scope = null)
    : base(new HttpClientHandler()) {
        _tokenEndpoint = tokenEndpoint;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _scope = scope ?? string.Empty;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct) {
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await GetTokenAsync(ct));
        return await base.SendAsync(request, ct);
    }

    private async Task<string> GetTokenAsync(CancellationToken ct) {
        if (_token is not null && DateTimeOffset.UtcNow < _expiry)
            return _token;

        await _lock.WaitAsync(ct);
        try {
            // double-check after acquiring the lock
            if (_token is not null && DateTimeOffset.UtcNow < _expiry)
                return _token;

            using var req = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint);
            var form = new Dictionary<string, string> {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
            };
            if (_scope.Length > 0) form["scope"] = _scope;
            req.Content = new FormUrlEncodedContent(form);

            using var resp = await base.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadFromJsonAsync<TokenResponse>(ct);
            _token = json!.AccessToken;
            _expiry = DateTimeOffset.UtcNow.AddSeconds(json.ExpiresIn - 30); // 30 s safety margin
            return _token;
        } finally { _lock.Release(); }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}