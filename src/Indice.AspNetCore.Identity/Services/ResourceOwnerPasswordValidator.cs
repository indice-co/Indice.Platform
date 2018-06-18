using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity.Services
{
    /// <summary>
    /// Validates username and password against the user store of <typeparamref name="TUser"/>
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public class ResourceOwnerPasswordValidator<TUser> : IResourceOwnerPasswordValidator where TUser : class
    {
        readonly UserManager<TUser> _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceOwnerPasswordValidator{TUser}"/> class.
        /// </summary>
        /// <param name="userManager">The AspNet Identity user manager.</param>
        public ResourceOwnerPasswordValidator(UserManager<TUser> userManager) => _userManager = userManager;

        /// <summary>
        /// Validates username and password against the user store
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context) {
            var user = await _userManager.FindByNameAsync(context.UserName);

            if (user != null && await _userManager.CheckPasswordAsync(user, context.Password)) {
                var subject = await _userManager.GetUserIdAsync(user);
                var claims = await _userManager.GetClaimsAsync(user);
                context.Result = new GrantValidationResult(subject, authenticationMethod: "password", claims: claims);

                return;
            }

            context.Result = new GrantValidationResult(OidcConstants.TokenErrors.InvalidGrant, "Invalid username or password.");
        }
    }
}
