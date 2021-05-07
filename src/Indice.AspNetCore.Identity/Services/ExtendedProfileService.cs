using System;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Indice.AspNetCore.Identity.Data.Models;

namespace Indice.AspNetCore.Identity.Services
{
    /// <summary>
    /// Extends the implementation of <see cref="IProfileService"/> and validates the user based on ASP.NET Identity and custom rules.
    /// </summary>
    /// <typeparam name="TUser">The type of user.</typeparam>
    public class ExtendedProfileService<TUser> : IProfileService where TUser : User
    {
        private readonly IProfileService _inner;
        private readonly ExtendedUserManager<TUser> _userManager;
        private readonly ExtendedSignInManager<TUser> _signInManager;

        /// <summary>
        /// Creates a new instance of <see cref="ExtendedProfileService{TUser}"/>.
        /// </summary>
        /// <param name="profileService"> This interface allows IdentityServer to connect to your user and profile store.</param>
        /// <param name="userManager"></param>
        /// <param name="signInManager"></param>
        public ExtendedProfileService(IProfileService profileService, ExtendedUserManager<TUser> userManager, ExtendedSignInManager<TUser> signInManager) {
            _inner = profileService ?? throw new ArgumentNullException(nameof(profileService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        }

        /// <inheritdoc />
        public Task GetProfileDataAsync(ProfileDataRequestContext context) => _inner.GetProfileDataAsync(context);

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
