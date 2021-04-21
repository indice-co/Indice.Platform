using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Identity
{
    /// <summary>
    /// Generate the claims for a user. Extends the default principal created by the IdentityServer with custom claims.
    /// </summary>
    public class ExtendedUserClaimsPrincipalFactory : ExtendedUserClaimsPrincipalFactory<User, IdentityRole>
    {
        /// <summary>
        /// Constructor for the extender user claims principal factory.
        /// </summary>
        /// <param name="userManager">The <see cref="UserManager{TUser}"/> to retrieve user information from.</param>
        /// <param name="roleManager">The <see cref="RoleManager{TRole}"/> to retrieve a user's roles from.</param>
        /// <param name="optionsAccessor">The configured <see cref="AppSetting"/>.</param>
        public ExtendedUserClaimsPrincipalFactory(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor) { }

        /// <summary>
        ///  Generates the claims for a user.
        /// </summary>
        /// <param name="user">The user to create a <see cref="ClaimsIdentity"/> from.</param>
        /// <returns>The claim-based identity of the user.</returns>
        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user) => await base.GenerateClaimsAsync(user);
    }

    /// <summary>
    /// Generate the claims for a user. Extends the default principal created by the IdentityServer with custom claims.
    /// </summary>
    public class ExtendedUserClaimsPrincipalFactory<TUser, TRole> : UserClaimsPrincipalFactory<TUser, TRole>
        where TUser : User
        where TRole : IdentityRole
    {
        const string ISOFORMAT = "yyyy-MM-dd\\THH:mm:ss.fffK"; //ISO-8601 used by Javascript (ALWAYS UTC)

        /// <summary>
        /// Constructor for the extender user claims principal factory.
        /// </summary>
        /// <param name="userManager">The <see cref="UserManager{TUser}"/> to retrieve user information from.</param>
        /// <param name="roleManager">The <see cref="RoleManager{TRole}"/> to retrieve a user's roles from.</param>
        /// <param name="optionsAccessor">The configured <see cref="AppSetting"/>.</param>
        public ExtendedUserClaimsPrincipalFactory(UserManager<TUser> userManager, RoleManager<TRole> roleManager, IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor) { }

        /// <summary>
        /// Generates the claims for a user.
        /// </summary>
        /// <param name="user">The user to create a <see cref="ClaimsIdentity"/> from.</param>
        /// <returns>The claim-based identity of the user.</returns>
        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(TUser user) {
            // https://github.com/aspnet/AspNetCore/blob/master/src/Identity/Extensions.Core/src/UserClaimsPrincipalFactory.cs#L135
            var identity = await base.GenerateClaimsAsync(user);
            var additionalClaims = new List<Claim>();
            if (!identity.HasClaim(x => x.Type == BasicClaimTypes.Admin)) {
                var isAdmin = user.Admin;
                if (!isAdmin) {
                    if (identity.HasClaim(x => x.Type == JwtClaimTypes.Role)) {
                        isAdmin = identity.HasClaim(JwtClaimTypes.Role, BasicRoleNames.Administrator);
                    } else {
                        var roles = (await UserManager.GetRolesAsync(user)).Select(role => new Claim(JwtClaimTypes.Role, role));
                        isAdmin = roles.Where(x => x.Value == BasicRoleNames.Administrator).Any();
                    }
                }
                additionalClaims.Add(new Claim(BasicClaimTypes.Admin, isAdmin.ToString().ToLower(), ClaimValueTypes.Boolean));
            }
            if (!identity.HasClaim(x => x.Type == BasicClaimTypes.PasswordExpirationDate) && user.PasswordExpirationDate.HasValue) {
                additionalClaims.Add(new Claim(BasicClaimTypes.PasswordExpirationDate, ToISOString(user.PasswordExpirationDate.Value.UtcDateTime), ClaimValueTypes.DateTime));
            }
            if (!identity.HasClaim(x => x.Type == BasicClaimTypes.PasswordExpirationPolicy) && user.PasswordExpirationPolicy.HasValue) {
                additionalClaims.Add(new Claim(BasicClaimTypes.PasswordExpirationPolicy, user.PasswordExpirationPolicy.ToString()));
            }
            identity.AddClaims(additionalClaims);
            return identity;
        }

        private static string ToISOString(DateTime dateTime, bool useLocal = false) {
            if (!useLocal && dateTime.Kind == DateTimeKind.Local) {
                // If d is LT or you don't want LocalTime -> convert to UTC and always add K format always add 'Z' postfix.
                return dateTime.ToUniversalTime().ToString(ISOFORMAT);
            } else {
                //If d is already UTC K format add 'Z' postfix, if d is LT K format add +/-TIMEOFFSET.
                return dateTime.ToString(ISOFORMAT);
            }
        }
    }
}
