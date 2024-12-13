using Indice.Features.Cases.Core;
using Indice.Features.Cases.Server;
using Indice.Features.Cases.Server.Authorization;
using Indice.Security;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods on <see cref="AuthorizationOptions"/>.</summary>
public static class MessagingAuthorizationExtensions
{
    /// <summary>Adds Cases authorization policy according to access level.</summary>
    /// <param name="options">Provides programmatic configuration used by <see cref="IAuthorizationService"/> and <see cref="IAuthorizationPolicyProvider"/>.</param>
    /// <param name="policyName">The name of the policy.</param>
    /// <param name="minimumAccessLevel">The minimum required access level</param>
    /// <param name="requiredScope">The scope required to operate. Defaults to <strong>messages</strong></param>
    public static void AddCasesAccessPolicy(this AuthorizationOptions options, string policyName, CasesAccessLevel minimumAccessLevel = CasesAccessLevel.Member, string requiredScope = CasesCoreConstants.DefaultScopeName) =>
        options.AddPolicy(policyName, policyBuilder => policyBuilder
                                                        .AddAuthenticationSchemes("Bearer")
                                                        .RequireAuthenticatedUser()
                                                        .RequireAssertion(x => x.User.HasScope(requiredScope))
                                                        .RequireCasesAccess(minimumAccessLevel));

    /// <summary>Requires cases management Authorization of some level.</summary>
    /// <param name="builder">Used for building policies during application startup.</param>
    /// <param name="minimumAccessLevel">The minimum required access level</param>
    public static AuthorizationPolicyBuilder RequireCasesAccess(this AuthorizationPolicyBuilder builder, CasesAccessLevel minimumAccessLevel = CasesAccessLevel.Member) => builder.AddRequirements(new CasesAccessRequirement(minimumAccessLevel));
}