using System;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Security;

namespace Indice.AspNetCore.Identity.Services
{
    /// <summary>
    /// Extends the implementation of <see cref="IProfileService"/> and validates the user based on ASP.NET Identity and custom rules.
    /// </summary>
    /// <typeparam name="TInner">The type is decorated.</typeparam>
    public class ExtendedProfileService<TInner> : IProfileService where TInner : IProfileService
    {
        private readonly IProfileService _inner;
        private readonly ExtendedUserManager<User> _userManager;
        private readonly ExtendedSignInManager<User> _signInManager;

        /// <summary>
        /// Creates a new instance of <see cref="ExtendedProfileService{TUser}"/>.
        /// </summary>
        /// <param name="profileService"> This interface allows IdentityServer to connect to your user and profile store.</param>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="signInManager">Provides the APIs for user sign in.</param>
        public ExtendedProfileService(TInner profileService, ExtendedUserManager<User> userManager, ExtendedSignInManager<User> signInManager) {
            _inner = profileService ?? throw new ArgumentNullException(nameof(profileService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        }

        /// <inheritdoc />
        public async Task GetProfileDataAsync(ProfileDataRequestContext context) {
            await _inner.GetProfileDataAsync(context);
            var otpVerifiedClaim = context.Subject.FindFirst(BasicClaimTypes.OtpAuthenticated);
            if (otpVerifiedClaim is not null) {
                context.IssuedClaims.Add(otpVerifiedClaim);
            }
        }

        /// <inheritdoc />
        public async Task IsActiveAsync(IsActiveContext context) {
            await _inner.IsActiveAsync(context);
            if (!context.IsActive) {
                return;
            }
            var subject = context.Subject.FindFirst(JwtClaimTypes.Subject)?.Value;
            if (string.IsNullOrWhiteSpace(subject)) {
                context.IsActive = true;
                return;
            }
            var user = await _userManager.FindByIdAsync(subject);
            var isActive = user != null &&
                !user.IsLockedOut() &&
                !user.HasExpiredPassword() &&
                !user.Blocked &&
                (!_signInManager.RequirePostSignInConfirmedEmail || user.EmailConfirmed) &&
                (!_signInManager.RequirePostSignInConfirmedPhoneNumber || user.PhoneNumberConfirmed);
            context.IsActive = isActive;
        }
    }
}
