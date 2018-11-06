using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using IdentityModel;
using Indice.AspNetCore.Identity.Models;
using Indice.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Identity.Services
{
    /// <summary>
    /// Generate the claims for a user. Extends the default principal created by the IdentityServer with any custom claims.
    /// </summary>
    public class ExtendedUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, IdentityRole>
    {
        private readonly UserManager<User> _userManager;

        /// <summary>
        /// Constructor for the extender user claims principal factory
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="roleManager"></param>
        /// <param name="optionsAccessor"></param>
        public ExtendedUserClaimsPrincipalFactory(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor) {

            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        /// <summary>
        ///  Generate the claims for a user.
        /// </summary>
        /// <param name="user">The user to create a <see cref="ClaimsIdentity"/> from.</param>
        /// <returns></returns>
        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user) {
            var identity = await base.GenerateClaimsAsync(user);
            var additionalClaims = new List<Claim>();
            if (!identity.HasClaim(x => x.Type == JwtClaimTypes.Role)) {
                var roles = (await _userManager.GetRolesAsync(user)).Select(role => new Claim(JwtClaimTypes.Role, role));
                additionalClaims.AddRange(roles);
            }
            if (!identity.HasClaim(x => x.Type == BasicClaimTypes.Admin)) {
                additionalClaims.Add(new Claim(BasicClaimTypes.Admin, user.Admin.ToString().ToLower(), ClaimValueTypes.Boolean));
            }
            identity.AddClaims(additionalClaims);
            return identity;
        }
    }
}
