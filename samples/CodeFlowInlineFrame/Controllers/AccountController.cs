using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CodeFlowInlineFrame.Configuration;
using CodeFlowInlineFrame.Models;
using CodeFlowInlineFrame.Settings;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CodeFlowInlineFrame.Controllers
{
    [Route("account")]
    public class AccountController : Controller
    {
        private readonly ClientSettings _clientSettings;
        private readonly GeneralSettings _generalSettings;
        private readonly IHttpClientFactory _httpClientFactory;
        public const string Name = "Account";

        public AccountController(IOptions<ClientSettings> clientSettings, IOptions<GeneralSettings> generalSettings, IHttpClientFactory httpClientFactory) {
            _clientSettings = clientSettings?.Value ?? throw new ArgumentNullException(nameof(clientSettings));
            _generalSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        [HttpGet("login")]
        public ViewResult Login([FromQuery]string returnUrl) {
            var authorizeEndpoint = $"{_generalSettings.Authority}/connect/authorize";
            var requestUrl = new RequestUrl(authorizeEndpoint);
            var codeVerifier = CryptoRandom.CreateUniqueId(32);
            TempData.Add(OidcConstants.TokenRequest.CodeVerifier, codeVerifier);
            string codeChallenge;
            using (var sha256 = SHA256.Create()) {
                var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                codeChallenge = Base64Url.Encode(challengeBytes);
            }
            var authorizeUrl = requestUrl.CreateAuthorizeUrl(
                clientId: _clientSettings.Id,
                responseType: OidcConstants.ResponseTypes.Code,
                codeChallengeMethod: OidcConstants.CodeChallengeMethods.Sha256,
                codeChallenge: codeChallenge,
                responseMode: OidcConstants.ResponseModes.Query,
                redirectUri: $"{_generalSettings.Host}/account/auth-callback",
                nonce: Guid.NewGuid().ToString(),
                scope: string.Join(" ", _clientSettings.Scopes),
                state: !string.IsNullOrEmpty(returnUrl) ? Convert.ToBase64String(Encoding.UTF8.GetBytes(returnUrl)) : null
            );
            return View(new LoginViewModel {
                AuthorizeUrl = authorizeUrl
            });
        }

        [HttpGet("auth-callback")]
        public async Task<ViewResult> AuthCallback() {
            var authorizationResponse = new AuthorizationResponse();
            authorizationResponse.PopulateFrom(HttpContext.Request.QueryString.Value);
            if (string.IsNullOrEmpty(authorizationResponse.Code)) {
                throw new Exception("Authorization code is not present in the response.");
            }
            var tokenEndpoint = $"{_generalSettings.Authority}/connect/token";
            TempData.TryGetValue(OidcConstants.TokenRequest.CodeVerifier, out var codeVerifier);
            var httpClient = _httpClientFactory.CreateClient(HttpClientNames.IdentityServer);
            var tokenResponse = await httpClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest {
                Address = tokenEndpoint,
                ClientId = _clientSettings.Id,
                Code = authorizationResponse.Code,
                RedirectUri = $"{_generalSettings.Host}/account/auth-callback",
                CodeVerifier = codeVerifier.ToString()
            });
            if (tokenResponse.IsError) {
                throw new Exception("There was an error retrieving the access token.", tokenResponse.Exception);
            }
            var userInfoResponse = await httpClient.GetUserInfoAsync(new UserInfoRequest {
                Address = $"{_generalSettings.Authority}/connect/userinfo",
                Token = tokenResponse.AccessToken
            });
            if (userInfoResponse.IsError) {
                throw new Exception("There was an error retrieving user information from authority.", userInfoResponse.Exception);
            }
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(userInfoResponse.Claims, "Cookies", JwtClaimTypes.Name, JwtClaimTypes.Role));
            var authenticationProperties = new AuthenticationProperties();
            authenticationProperties.StoreTokens(new List<AuthenticationToken> {
                new AuthenticationToken {
                    Name = "access_token",
                    Value = tokenResponse.AccessToken
                },
                new AuthenticationToken {
                    Name = "refresh_token",
                    Value = tokenResponse.RefreshToken
                },
                new AuthenticationToken {
                    Name = "expires_at",
                    Value = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn).ToString("o", CultureInfo.InvariantCulture)
                }
            });
            await HttpContext.SignInAsync(claimsPrincipal, authenticationProperties);
            var returnUrl = "/";
            if (!string.IsNullOrEmpty(authorizationResponse.State)) {
                returnUrl = Encoding.UTF8.GetString(Convert.FromBase64String(authorizationResponse.State));
            }
            return View("Redirect", new RedirectViewModel {
                Url = returnUrl
            });
        }
    }
}
