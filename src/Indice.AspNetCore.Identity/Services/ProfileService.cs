using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Indice.AspNetCore.Identity.Models;
using Indice.Security;
using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity.Services
{
    /// <summary>
    /// 
    /// </summary>
    public class UserProfileService : ProfileService<User>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileService{TUser}"/> class.
        /// </summary>
        /// <param name="userManager">The AspNet Identity user manager.</param>
        public UserProfileService(UserManager<User> userManager) : base(userManager) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        protected override async Task<List<Claim>> GetClaimsAsync(User user) {
            var claims = await base.GetClaimsAsync(user);

            if (user.Admin) {
                claims.Add(new Claim(BasicClaimTypes.Admin, $"{user.Admin}".ToLower(), ClaimValueTypes.Boolean));
            }

            return claims;
        }
    }

    /// <summary>
    /// Profile service to use with IdentityServer. Converts a user and his stored claims to a claims principal. 
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public class ProfileService<TUser> : IProfileService where TUser : class, new()
    {
        readonly UserManager<TUser> _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileService{TUser}"/> class.
        /// </summary>
        /// <param name="userManager">The AspNet Identity user manager.</param>
        public ProfileService(UserManager<TUser> userManager) => _userManager = userManager;

        /// <summary>
        /// This method is called whenever claims about the user are requested (e.g. during token creation or via the userinfo endpoint).
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public async Task GetProfileDataAsync(ProfileDataRequestContext context) {
            var user = await _userManager.FindByIdAsync(context.Subject.GetSubjectId());
            var claims = await GetClaimsAsync(user);
            context.AddRequestedClaims(claims);
        }

        /// <summary>
        /// This method gets called whenever identity server needs to determine if the user is valid or active (e.g. during token issuance or validation).
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Subject</exception>
        public async Task IsActiveAsync(IsActiveContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Subject == null) {
                throw new ArgumentNullException(nameof(context.Subject));
            }

            context.IsActive = false;
            var subject = context.Subject;
            var user = await _userManager.FindByIdAsync(context.Subject.GetSubjectId());

            if (user != null) {
                var securityStampChanged = false;

                if (_userManager.SupportsUserSecurityStamp) {
                    var securityStamp = (from claim in subject.Claims
                                         where claim.Type == "security_stamp"
                                         select claim.Value).SingleOrDefault();

                    if (securityStamp != null) {
                        var latest_security_stamp = await _userManager.GetSecurityStampAsync(user);
                        securityStampChanged = securityStamp != latest_security_stamp;
                    }
                }

                context.IsActive = !securityStampChanged && !await _userManager.IsLockedOutAsync(user);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        protected virtual async Task<List<Claim>> GetClaimsAsync(TUser user) {
            var claims = new List<Claim> {
                new Claim(JwtClaimTypes.Subject, await _userManager.GetUserIdAsync(user)),
                new Claim(JwtClaimTypes.Name, await _userManager.GetUserNameAsync(user))
            };

            if (_userManager.SupportsUserEmail) {
                var email = await _userManager.GetEmailAsync(user);

                if (!string.IsNullOrWhiteSpace(email)) {
                    claims.AddRange(new[] {
                        new Claim(JwtClaimTypes.Email, email),
                        new Claim(JwtClaimTypes.EmailVerified, await _userManager.IsEmailConfirmedAsync(user) ? "true" : "false", ClaimValueTypes.Boolean)
                    });
                }
            }

            if (_userManager.SupportsUserPhoneNumber) {
                var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

                if (!string.IsNullOrWhiteSpace(phoneNumber)) {
                    claims.AddRange(new[] {
                        new Claim(JwtClaimTypes.PhoneNumber, phoneNumber),
                        new Claim(JwtClaimTypes.PhoneNumberVerified, await _userManager.IsPhoneNumberConfirmedAsync(user) ? "true" : "false", ClaimValueTypes.Boolean)
                    });
                }
            }

            if (_userManager.SupportsUserClaim) {
                claims.AddRange(await _userManager.GetClaimsAsync(user));
            }

            if (_userManager.SupportsUserRole) {
                var roles = await _userManager.GetRolesAsync(user);
                claims.AddRange(roles.Select(role => new Claim(JwtClaimTypes.Role, role)));
            }

            var customUser = user as User;

            if (customUser != null && customUser.Admin) {
                if (customUser.Admin) {
                    claims.Add(new Claim(BasicClaimTypes.Admin, $"{customUser.Admin}".ToLower(), ClaimValueTypes.Boolean));
                }

                if (customUser.FirstName) {
                    claims.Add(new Claim(BasicClaimTypes.GivenName, $"{customUser.FirstName}".ToLower(), ClaimValueTypes.Boolean));
                }

                if (customUser.LastName) {
                    claims.Add(new Claim(BasicClaimTypes.FamilyName, $"{customUser.LastName}".ToLower(), ClaimValueTypes.Boolean));
                }
            }

            if (customUser != null && customUser.Admin) {
                claims.Add(new Claim(BasicClaimTypes.Admin, $"{customUser.Admin}".ToLower(), ClaimValueTypes.Boolean));
            }

            return claims;
        }
    }
}
