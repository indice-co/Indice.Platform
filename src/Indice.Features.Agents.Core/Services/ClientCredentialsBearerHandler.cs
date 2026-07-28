using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Azure;
using Indice.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;

namespace Indice.Features.Agents.Core.Services;

internal sealed class ClientCredentialsBearerHandler : DelegatingHandler
{
    private readonly string _tokenEndpoint;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _scope;
    private readonly IHttpContextAccessor _contextAccessor;


    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly IDistributedCache _cache;

    public ClientCredentialsBearerHandler(
        string tokenEndpoint, string clientId, string clientSecret, string scope, IHttpContextAccessor contextAccessor, IDistributedCache cache)
    : base(new HttpClientHandler()) {
        _tokenEndpoint = tokenEndpoint;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _contextAccessor = contextAccessor;
        _cache = cache;
        _scope = scope ?? string.Empty;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct) {
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await GetTokenAsync(ct));
        return await base.SendAsync(request, ct);
    }

    private async Task<string> GetTokenAsync(CancellationToken ct) {

        await _lock.WaitAsync(ct);
        try {
            // double-check after acquiring the lock
            var currentAccessToken = ResolveAuthorizationHeaderValue();
            var currentSubjectId = _contextAccessor.HttpContext?.User.FindSubjectId();
            var userPresent = !string.IsNullOrWhiteSpace(currentAccessToken) && !string.IsNullOrWhiteSpace(currentSubjectId);
            var grantType = userPresent ? "delegation" : "client_credentials";
            var cacheKey = $"{_clientId}|{grantType}|sub|{currentSubjectId}";
            var accessToken = await _cache.GetStringAsync(cacheKey);
            if (accessToken != null) {
                return accessToken;
            }
            using var req = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint);
            var form = new Dictionary<string, string> {
                ["grant_type"] = grantType,
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
                ["scope"] = _scope
            };
            req.Content = new FormUrlEncodedContent(form);

            using var resp = await base.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadFromJsonAsync<TokenResponse>(ct);
            await _cache.SetStringAsync(cacheKey, json!.AccessToken, new DistributedCacheEntryOptions {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(json.ExpiresIn - 30)
            });
            return json.AccessToken!;
        } finally { _lock.Release(); }
    }

    /// <summary>
    /// Gets the access token from the current value of the <strong>Authorization</strong> header next to the scheme.
    /// </summary>
    private string? ResolveAuthorizationHeaderValue() {
        var authHeader = _contextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (authHeader != null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) {
            return authHeader["Bearer ".Length..];
        }
        return null;
    }
    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}