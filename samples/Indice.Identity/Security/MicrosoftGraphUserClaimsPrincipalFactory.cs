using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Security;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Indice.Identity.Security
{
    public class MicrosoftGraphUserClaimsPrincipalFactory : ExtendedUserClaimsPrincipalFactory<User, Role>
    {
        private readonly UserManager<User> _userManager;

        public MicrosoftGraphUserClaimsPrincipalFactory(UserManager<User> userManager, RoleManager<Role> roleManager, IOptions<IdentityOptions> options) : base(userManager, roleManager, options) {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        protected async override Task<ClaimsIdentity> GenerateClaimsAsync(User user) {
            var identity = await base.GenerateClaimsAsync(user);
            var msGraphToken = await _userManager.GetAuthenticationTokenAsync(user, MicrosoftAccountDefaults.AuthenticationScheme, OpenIdConnectParameterNames.AccessToken);
            if (!string.IsNullOrEmpty(msGraphToken)) {
                identity.AddClaim(new Claim(BasicClaimTypes.MsGraphToken, msGraphToken));
            }
            return identity;
        }
    }
}
