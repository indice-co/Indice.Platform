using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity.Extensions;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Identity.Controllers
{
    /// <summary>
    /// Contains methods that help a user to login using external providers.
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    [SecurityHeaders]
    public class ExternalController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IEventService _events;
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        /// <summary>
        /// 
        /// </summary>
        public const string Name = "External";

        public ExternalController(IIdentityServerInteractionService interaction, IClientStore clientStore, IEventService events, SignInManager<User> signInManager, UserManager<User> userManager) {
            _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
            _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        [HttpGet]
        public async Task<IActionResult> Challenge(string provider, string returnUrl) {
            if (string.IsNullOrEmpty(returnUrl)) {
                returnUrl = "~/";
            }
            // Validate returnUrl - either it is a valid OIDC URL or back to a local page.
            if (Url.IsLocalUrl(returnUrl) == false && _interaction.IsValidReturnUrl(returnUrl) == false) {
                // User might have clicked on a malicious link - should be logged.
                throw new Exception("Invalid return URL.");
            }
            if (AccountOptions.WindowsAuthenticationSchemeName == provider) {
                // Windows authentication needs special handling.
                return await ProcessWindowsLoginAsync(returnUrl);
            }
            var authenticationProperties = _signInManager.ConfigureExternalAuthenticationProperties(provider, Url.Action(nameof(Callback), new { returnUrl }));
            authenticationProperties.Items.Add(nameof(returnUrl), returnUrl);
            return Challenge(authenticationProperties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> Callback(string returnUrl) {
            if (string.IsNullOrEmpty(returnUrl)) {
                returnUrl = "~/";
            };
            // Validate returnUrl - either it is a valid OIDC URL or back to a local page.
            if (Url.IsLocalUrl(returnUrl) == false && _interaction.IsValidReturnUrl(returnUrl) == false) {
                // User might have clicked on a malicious link - should be logged.
                throw new Exception("Invalid return URL.");
            }
            // Read external identity from the temporary cookie.
            // At this point registered claims transformation will run (the ClaimsTransformer class).
            // This is triggered when HttpContext.AuthenticateAsync method is called (which is done inside GetExternalLoginInfoAsync method of SignInManager).
            var externalLoginInfo = await _signInManager.GetExternalLoginInfoAsync();
            if (externalLoginInfo == null) {
                throw new Exception($"Cannot read external login information from {externalLoginInfo.LoginProvider}.");
            }
            var user = await _userManager.FindByLoginAsync(externalLoginInfo.LoginProvider, externalLoginInfo.ProviderKey);
            // If user with the specified login is null, this means that we have to do with a brand new user.
            if (user == null) {
                // Retrieve claims of the external user.
                var claims = externalLoginInfo.Principal.Claims.ToList();
                // We can choose to auto-provision the user or initiate a custom workflow for user registration.
                user = await AutoProvisionExternalUserAsync(Guid.NewGuid().ToString(), claims);
                // Save user external login.
                await _userManager.AddLoginAsync(user, externalLoginInfo);
            }
            await _events.RaiseAsync(new UserLoginSuccessEvent(externalLoginInfo.LoginProvider, externalLoginInfo.Principal.GetSubjectId(), user.Id, user.UserName));
            // Save user tokes retrieved from external provider.
            await _signInManager.UpdateExternalAuthenticationTokensAsync(externalLoginInfo);
            await _signInManager.ExternalLoginSignInAsync(externalLoginInfo.LoginProvider, externalLoginInfo.ProviderKey, isPersistent: true);
            // Check if external login is in the context of an OIDC request.
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context != null && await _clientStore.IsPkceClientAsync(context.ClientId)) {
                // If the client is PKCE then we assume it's native, so this change in how to return the response is for better UX for the end user.
                return View("Redirect", new RedirectViewModel {
                    RedirectUrl = returnUrl
                });
            }
            return Redirect(returnUrl);
        }

        private async Task<IActionResult> ProcessWindowsLoginAsync(string returnUrl) {
            // See if windows auth has already been requested and succeeded.
            var result = await HttpContext.AuthenticateAsync(AccountOptions.WindowsAuthenticationSchemeName);
            if (!(result?.Principal is WindowsPrincipal principal)) {
                // Trigger windows auth.
                // Since they don't support the redirect uri, so this URL is re-triggered when we call challenge.
                return Challenge(AccountOptions.WindowsAuthenticationSchemeName);
            }
            // We will issue the external cookie and then redirect the user back to the external callback, in essence, treating windows auth the same as any other external authentication mechanism.
            var authenticationProperties = _signInManager.ConfigureExternalAuthenticationProperties(AccountOptions.WindowsAuthenticationSchemeName, Url.Action(nameof(Callback), new { returnUrl }));
            authenticationProperties.Items.Add(nameof(returnUrl), returnUrl);
            var claimsIdentity = new ClaimsIdentity(AccountOptions.WindowsAuthenticationSchemeName);
            claimsIdentity.AddClaim(new Claim(JwtClaimTypes.Subject, principal.Identity.Name));
            claimsIdentity.AddClaim(new Claim(JwtClaimTypes.Name, principal.Identity.Name));
            // Add the groups as claims - be careful if the number of groups is too large.
            if (AccountOptions.IncludeWindowsGroups) {
                var windowsIdentity = principal.Identity as WindowsIdentity;
                var groups = windowsIdentity?.Groups?.Translate(typeof(NTAccount));
                if (groups != null) {
                    var roles = groups.Select(x => new Claim(JwtClaimTypes.Role, x.Value));
                    claimsIdentity.AddClaims(roles);
                }
            }
            await HttpContext.SignInAsync(IdentityConstants.ExternalScheme, new ClaimsPrincipal(claimsIdentity), authenticationProperties);
            return Redirect(authenticationProperties.RedirectUri);
        }

        private async Task<User> AutoProvisionExternalUserAsync(string userId, List<Claim> claims) {
            var email = claims.Single(x => x.Type == JwtClaimTypes.Email).Value;
            // New user auto-registration flow.
            var user = new User(email, userId) {
                Email = email,
                EmailConfirmed = true
            };
            // Add claims from the external provider.
            foreach (var claim in claims) {
                if (claim.Type == JwtClaimTypes.Subject || claim.Type == JwtClaimTypes.Email) {
                    continue;
                }
                user.Claims.Add(new IdentityUserClaim<string> {
                    ClaimType = claim.Type,
                    ClaimValue = claim.Value,
                    UserId = userId
                });
            }
            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded) {
                throw new Exception("Failed to automatically create external user.");
            }
            return user;
        }
    }
}
