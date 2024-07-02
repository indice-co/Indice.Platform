using IdentityModel;
using IdentityModel.Client;
using Indice.Features.GovGr.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Indice.Features.GovGr.Http;

/// <summary>
/// HTTP Delegating Handler responsible to set an AccessToken for a Bancapp G-Cloud request
/// </summary>
public class BancappAuthorizationMessageHandler : DelegatingHandler
{
    private const string TenantNameProd = "bancappaade";
    private const string TenantNameStage = "myhands2testaade";
    private const int TokenExpirationInSeconds = 30;
    
    private readonly GovGrOptions.BancappOptions _options;
    private readonly IDistributedCache _cache;
    private readonly HttpClient _httpClient;
    private string Scope => $"openid {_options.ClientId} offline_access";
    private string AccessTokenKey => $"Bancapp|{Scope}|{_options.ClientId}|access-token";
    private string RefreshTokenKey => $"Bancapp|{Scope}|{_options.ClientId}|refresh-token";
    private string AuthTenant => _options.IsProduction ? TenantNameProd : TenantNameStage;
    private string AuthUrl => $"https://{AuthTenant}.b2clogin.com/{AuthTenant}.onmicrosoft.com/B2C_1_ropcauth/oauth2/v2.0/token";

    /// <summary>Authorization Handler for <see cref="GovGrBancappClient"/>.</summary>
    /// <exception cref="ArgumentNullException"></exception>
    public BancappAuthorizationMessageHandler(IDistributedCache cache, HttpClient httpClient, IOptions<GovGrOptions> options) {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value.Bancapp ?? throw new ArgumentNullException(nameof(options));
    }
    
    /// <summary>Performs the request after setting the access token.</summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        request.SetBearerToken(await GetAccessToken());
        return await base.SendAsync(request, cancellationToken);
    }
    
    private async Task<string> GetAccessToken() {
        var accessToken = await _cache.GetStringAsync(AccessTokenKey);
        if (!string.IsNullOrWhiteSpace(accessToken)) {
            return accessToken;
        }

        var refreshToken = await _cache.GetStringAsync(RefreshTokenKey);
        if (!string.IsNullOrWhiteSpace(refreshToken)) {
            var refreshTokenResponse = await _httpClient.RequestRefreshTokenAsync(new RefreshTokenRequest {
                RefreshToken = refreshToken,
                Scope = Scope,
                Address = AuthUrl,
                GrantType = OidcConstants.GrantTypes.RefreshToken,
                Resource = new[] { _options.ClientId },
                Parameters = new Parameters { { OidcConstants.TokenRequest.ClientId, _options.ClientId } }
            });

            return await TryGetAccessToken(refreshTokenResponse);
        }

        var passwordTokenResponse = await _httpClient.RequestPasswordTokenAsync(new PasswordTokenRequest {
            UserName = _options.Username,
            Password = _options.Password,
            Scope = Scope,
            Address = AuthUrl,
            GrantType = OidcConstants.GrantTypes.Password,
            Parameters = new Parameters { { OidcConstants.TokenRequest.ClientId, _options.ClientId } }
        });

        return await TryGetAccessToken(passwordTokenResponse);
    }
    
    private async Task<string> TryGetAccessToken(TokenResponse response) {
        if (response.IsError) {
            throw new Exception($"{response.Error} : {response.ErrorDescription}");
        }

        await _cache.SetStringAsync(AccessTokenKey, response.AccessToken!, new DistributedCacheEntryOptions {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn - TokenExpirationInSeconds)
        });
        await _cache.SetStringAsync(RefreshTokenKey, response.RefreshToken!, new DistributedCacheEntryOptions {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn - TokenExpirationInSeconds)
        });

        return response.AccessToken;
    }
}