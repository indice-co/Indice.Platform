using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Identity
{
    /// <summary>An implementation of <see cref="IRememberTwoFactorClientProvider{TUser}"/> where the client (browser) is remembered by using a cookie.</summary>
    /// <typeparam name="TUser">The user entity.</typeparam>
    public class RememberTwoFactorClientCookie<TUser> : IRememberTwoFactorClientProvider<TUser> where TUser : User
    {
        private readonly ExtendedUserManager<TUser> _userManager;
        private readonly IOptionsSnapshot<IdentityOptions> _optionsAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>Creates a new instance of <see cref="RememberTwoFactorClientCookie{TUser}"/> class.</summary>
        /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
        /// <param name="optionsAccessor"></param>
        /// <param name="httpContextAccessor"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public RememberTwoFactorClientCookie(
            ExtendedUserManager<TUser> userManager,
            IOptionsSnapshot<IdentityOptions> optionsAccessor,
            IHttpContextAccessor httpContextAccessor) {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _optionsAccessor = optionsAccessor ?? throw new ArgumentNullException(nameof(optionsAccessor));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <inheritdoc />
        public async Task RememberTwoFactorClientAsync(TUser user) {
            var principal = await StoreRememberClient(user);
            await _httpContextAccessor.HttpContext.SignInAsync(IdentityConstants.TwoFactorRememberMeScheme, principal, new AuthenticationProperties { IsPersistent = true });
        }

        private async Task<ClaimsPrincipal> StoreRememberClient(TUser user) {
            var userId = await _userManager.GetUserIdAsync(user);
            var rememberBrowserIdentity = new ClaimsIdentity(IdentityConstants.TwoFactorRememberMeScheme);
            rememberBrowserIdentity.AddClaim(new Claim(ClaimTypes.Name, userId));
            if (_userManager.SupportsUserSecurityStamp) {
                var stamp = await _userManager.GetSecurityStampAsync(user);
                rememberBrowserIdentity.AddClaim(new Claim(_optionsAccessor.Value.ClaimsIdentity.SecurityStampClaimType, stamp));
            }
            return new ClaimsPrincipal(rememberBrowserIdentity);
        }
    }
}
