using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Indice.AspNetCore.MultiTenancy;
using Indice.AspNetCore.MultiTenancy.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods on <see cref="AuthorizationOptions"/>
    /// </summary>
    public static class AuthorizationOptionsExtensions
    {
        /// <summary>
        /// Add tenant member Authorization according to accessLevel.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="policyName"></param>
        /// <param name="accessLevel"></param>
        public static void AddTenantMemberPolicy(this AuthorizationOptions options, string policyName, int accessLevel = 0) {
            options.AddPolicy(policyName, policyBuilder => policyBuilder.RequireAuthenticatedUser()
                                                                        .RequireTenantMembership(accessLevel));
        }

        /// <summary>
        /// Add tenant member Authorization according to accessLevel.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="accessLevel"></param>
        public static AuthorizationPolicyBuilder RequireTenantMembership(this AuthorizationPolicyBuilder builder, int accessLevel = 0)
            => builder.AddRequirements(new BeTenantMemberRequirement(accessLevel));

        /// <summary>
        /// Checks if the current principal is a client owned by the system.
        /// </summary>
        /// <param name="principal">The current principal.</param>
        /// <returns></returns>
        internal static bool IsSystemClient(this ClaimsPrincipal principal) {
            var isSystem = principal.FindFirstValue($"client_{JwtClaimTypesInternal.System}") ?? 
                           principal.FindFirstValue(JwtClaimTypesInternal.System);
            return isSystem?.ToLower() == bool.TrueString.ToLower();
        }
    }
}
