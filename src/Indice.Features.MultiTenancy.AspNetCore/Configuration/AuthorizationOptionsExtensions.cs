using System.Security.Claims;
using Indice.Features.Multitenancy.AspNetCore;
using Indice.Features.Multitenancy.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods on <see cref="AuthorizationOptions"/>.</summary>
public static class AuthorizationOptionsExtensions
{
    /// <summary>Adds tenant member authorization policy according to access level.</summary>
    /// <param name="options">Provides programmatic configuration used by <see cref="IAuthorizationService"/> and <see cref="IAuthorizationPolicyProvider"/>.</param>
    /// <param name="policyName">The name of the policy.</param>
    /// <param name="accessLevel">The minimum access level required.</param>
    public static void AddTenantMemberPolicy(this AuthorizationOptions options, string policyName, int accessLevel = 0) => 
        options.AddPolicy(policyName, policyBuilder => policyBuilder.RequireAuthenticatedUser().RequireTenantMembership(accessLevel));

    /// <summary>Adds tenant member Authorization according to access level.</summary>
    /// <param name="builder">Used for building policies during application startup.</param>
    /// <param name="accessLevel">The minimum access level required.</param>
    public static AuthorizationPolicyBuilder RequireTenantMembership(this AuthorizationPolicyBuilder builder, int accessLevel = 0) => builder.AddRequirements(new BeTenantMemberRequirement(accessLevel));

    internal static bool IsSystemClient(this ClaimsPrincipal principal) {
        var isSystem = principal.FindFirstValue($"client_{JwtClaimTypesInternal.System}") ?? principal.FindFirstValue(JwtClaimTypesInternal.System);
        return isSystem?.ToLower() == bool.TrueString.ToLower();
    }
}
