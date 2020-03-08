using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ResourceOwnerPasswordFlow.Configuration;
using ResourceOwnerPasswordFlow.Models;
using ResourceOwnerPasswordFlow.Settings;

namespace ResourceOwnerPasswordFlow.Controllers
{
    [Route("account")]
    public class AccountController : Controller
    {
        private readonly ClientSettings _clientSettings;
        private readonly IHttpClientFactory _httpClientFactory;
        public const string Name = "Account";

        public AccountController(IOptions<ClientSettings> clientSettings, IHttpClientFactory httpClientFactory) {
            _clientSettings = clientSettings?.Value ?? throw new ArgumentNullException(nameof(clientSettings));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        [HttpGet("login")]
        public ViewResult Login([FromQuery]string returnUrl) {
            return View(new LoginViewModel {
                ReturnUrl = returnUrl
            });
        }

        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromForm]LoginViewModel model) {
            if (!ModelState.IsValid) {
                return View(model);
            }
            var httpClient = _httpClientFactory.CreateClient(HttpClientNames.IdentityServer);
            var tokenResponse = await httpClient.RequestPasswordTokenAsync(new PasswordTokenRequest {
                Address = $"{httpClient.BaseAddress.OriginalString}/connect/token",
                ClientId = _clientSettings.Id,
                ClientSecret = _clientSettings.Secret,
                Scope = string.Join(" ", _clientSettings.Scopes),
                UserName = model.Email,
                Password = model.Password
            });
            if (tokenResponse.IsError) {
                return View();
            }
            var userInfoResponse = await httpClient.GetUserInfoAsync(new UserInfoRequest {
                Address = $"{httpClient.BaseAddress.OriginalString}/connect/userinfo",
                Token = tokenResponse.AccessToken
            });
            if (userInfoResponse.IsError) {
                return View();
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
            if (Url.IsLocalUrl(model.ReturnUrl)) {
                return Redirect(model.ReturnUrl);
            } else {
                return Redirect("/");
            }
        }

        [HttpGet("access-denied")]
        public ViewResult AccessDenied() {
            return View();
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout() {
            await HttpContext.SignOutAsync();
            return Redirect("/");
        }
    }
}
