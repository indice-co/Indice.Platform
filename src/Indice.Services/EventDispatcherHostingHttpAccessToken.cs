using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Indice.Services;

/// <summary>Obtains and caches a client credentials access token for <see cref="EventDispatcherHostingHttp"/>.</summary>
internal sealed class EventDispatcherHostingHttpTokenProvider
{
    private static readonly TimeSpan RefreshBeforeExpiration = TimeSpan.FromSeconds(60);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _tokenHttpClientName;
    private readonly EventDispatcherHostingHttpOptions _options;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private volatile CachedToken? _token;

    public EventDispatcherHostingHttpTokenProvider(IHttpClientFactory httpClientFactory, string tokenHttpClientName, EventDispatcherHostingHttpOptions options) {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _tokenHttpClientName = tokenHttpClientName ?? throw new ArgumentNullException(nameof(tokenHttpClientName));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_options.ClientId);

    public async Task<string> GetAccessToken(CancellationToken cancellationToken) {
        var cached = _token;
        if (cached?.IsValid == true) {
            return cached.AccessToken;
        }
        await _lock.WaitAsync(cancellationToken);
        try {
            cached = _token;
            if (cached?.IsValid == true) {
                return cached.AccessToken;
            }
            var tokenEndpoint = _options.GetTokenEndpoint() ?? throw new InvalidOperationException($"Neither the {nameof(EventDispatcherHostingHttpOptions.Authority)} nor the {nameof(EventDispatcherHostingHttpOptions.TokenEndpoint)} of the workers API client is configured.");
            var form = new Dictionary<string, string> {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId!
            };
            if (!string.IsNullOrEmpty(_options.ClientSecret)) {
                form["client_secret"] = _options.ClientSecret;
            }
            if (!string.IsNullOrWhiteSpace(_options.Scope)) {
                form["scope"] = _options.Scope;
            }
            var httpClient = _httpClientFactory.CreateClient(_tokenHttpClientName); 
            using var content = new FormUrlEncodedContent(form);
            using var response = await httpClient.PostAsync(tokenEndpoint, content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode) {
                throw new HttpRequestException($"Failed to obtain an access token for the workers API from '{tokenEndpoint}'. The token endpoint responded with {(int)response.StatusCode} ({response.StatusCode}): {body}", null, response.StatusCode);
            }
            using var document = JsonDocument.Parse(body);
            var accessToken = document.RootElement.TryGetProperty("access_token", out var token) ? token.GetString() : null;
            if (string.IsNullOrEmpty(accessToken)) {
                throw new HttpRequestException($"The token endpoint '{tokenEndpoint}' did not return an access token.");
            }
            var expiresIn = document.RootElement.TryGetProperty("expires_in", out var expires) && expires.TryGetInt32(out var seconds) ? seconds : 3600;
            var now = DateTimeOffset.UtcNow;
            var refreshMargin = TimeSpan.FromSeconds(Math.Min(RefreshBeforeExpiration.TotalSeconds, Math.Max(expiresIn, 0) / 10d));
            _token = new CachedToken(accessToken, now.AddSeconds(expiresIn) - refreshMargin);
            return accessToken;
        } finally {
            _lock.Release();
        }
    }

    /// <summary>Discards the cached token, if it is the given one.</summary>
    public void Invalidate(string accessToken) {
        var cached = _token;
        if (cached?.AccessToken == accessToken) {
            Interlocked.CompareExchange(ref _token, null, cached);
        }
    }

    private sealed record CachedToken(string AccessToken, DateTimeOffset RefreshAt)
    {
        public bool IsValid => DateTimeOffset.UtcNow < RefreshAt;
    }
}

/// <summary>Attaches the client credentials access token to the requests of <see cref="EventDispatcherHostingHttp"/>. On a 401 it renews the token and retries once.</summary>
internal sealed class EventDispatcherHostingHttpTokenHandler : DelegatingHandler
{
    private readonly EventDispatcherHostingHttpTokenProvider _tokenProvider;

    public EventDispatcherHostingHttpTokenHandler(EventDispatcherHostingHttpTokenProvider tokenProvider) {
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        if (!_tokenProvider.IsEnabled) {
            return await base.SendAsync(request, cancellationToken);
        }
        var accessToken = await _tokenProvider.GetAccessToken(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        // Buffer the content, so the request can be sent again when the token is rejected.
        if (request.Content is not null) {
            await request.Content.LoadIntoBufferAsync();
        }
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized) {
            return response;
        }
        // The token may have been revoked or the signing keys rotated.
        _tokenProvider.Invalidate(accessToken);
        response.Dispose();
        accessToken = await _tokenProvider.GetAccessToken(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
