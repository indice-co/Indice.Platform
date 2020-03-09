using System;
using System.Security.Cryptography;
using System.Text;
using CodeFlowInlineFrame.Models;
using CodeFlowInlineFrame.Settings;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CodeFlowInlineFrame.Controllers
{
    [Route("account")]
    public class AccountController : Controller
    {
        private readonly ClientSettings _clientSettings;
        private readonly GeneralSettings _generalSettings;
        public const string Name = "Account";

        public AccountController(IOptions<ClientSettings> clientSettings, IOptions<GeneralSettings> generalSettings) {
            _clientSettings = clientSettings?.Value ?? throw new ArgumentNullException(nameof(clientSettings));
            _generalSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
        }

        [HttpGet("login")]
        public ViewResult Login([FromQuery]string returnUrl) {
            var authorizeEndpoint = $"{_generalSettings.Authority}/connect/authorize";
            var requestUrl = new RequestUrl(authorizeEndpoint);
            var codeVerifier = CryptoRandom.CreateUniqueId(32);
            HttpContext.Items.Add(OidcConstants.TokenRequest.CodeVerifier, codeVerifier);
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
        public ViewResult AuthCallback() {
            var authorizationResponse = new AuthorizationResponse();
            authorizationResponse.PopulateFrom(HttpContext.Request.QueryString.Value);
            return View();
        }
    }
}
