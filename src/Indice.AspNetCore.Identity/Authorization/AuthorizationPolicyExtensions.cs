using System;
using System.Collections.Generic;
using System.Text;
using Indice.Security;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Authorization
{
    /// <summary>
    /// Authorization policy extensions for <see cref="AuthorizationPolicyBuilder"/>
    /// </summary>
    public static class AuthorizationPolicyExtensions
    {
        /// <summary>
        /// Set up the <paramref name="policyBuilder"/> to require an administrator.
        /// </summary>
        /// <param name="policyBuilder">The policy builder</param>
        /// <returns></returns>
        public static AuthorizationPolicyBuilder RequireAdmin(this AuthorizationPolicyBuilder policyBuilder) {
            policyBuilder.RequireAuthenticatedUser();
            policyBuilder.RequireAssertion(context => {
                if (context.HasFailed) {
                    return true;
                }
                return context.User.IsAdmin();
            });
            return policyBuilder;
        }
    }
}
