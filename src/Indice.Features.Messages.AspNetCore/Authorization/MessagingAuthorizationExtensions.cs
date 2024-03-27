using Indice.Features.Messages.AspNetCore.Authorization;
using Indice.Features.Messages.Core;
using Indice.Security;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods on <see cref="AuthorizationOptions"/>.</summary>
public static class MessagingAuthorizationExtensions
{
    /// <summary>Adds tenant member authorization policy according to access level.</summary>
    /// <param name="options">Provides programmatic configuration used by <see cref="IAuthorizationService"/> and <see cref="IAuthorizationPolicyProvider"/>.</param>
    /// <param name="requiredScope">The scope required to operate. Defaults to <strong>messages</strong></param>
    public static void AddCampaignsManagementPolicy(this AuthorizationOptions options, string requiredScope = MessagesApi.Scope) =>
        options.AddPolicy(BeCampaignsManagerRequirement.PolicyName, policyBuilder => policyBuilder
                                                                    .AddAuthenticationSchemes(MessagesApi.AuthenticationScheme)
                                                                    .RequireAuthenticatedUser()
                                                                    .RequireAssertion(x => x.User.HasScope(requiredScope))
                                                                    .RequireCampaignsManagement());

    /// <summary>Adds tenant member authorization policy according to access level.</summary>
    /// <param name="options">Provides programmatic configuration used by <see cref="IAuthorizationService"/> and <see cref="IAuthorizationPolicyProvider"/>.</param>
    /// <param name="accessLevel">The minimum access level required.</param>
    /// <param name="requiredScope">The scope required to operate. Defaults to <strong>messages</strong></param>
    public static void AddMultitenantCampaignsManagementPolicy(this AuthorizationOptions options, int accessLevel, string requiredScope = MessagesApi.Scope) =>
        options.AddPolicy(BeCampaignsManagerRequirement.PolicyName, policyBuilder => policyBuilder
                                                                    .AddAuthenticationSchemes(MessagesApi.AuthenticationScheme)
                                                                    .RequireAuthenticatedUser()
                                                                    .RequireAssertion(x => x.User.HasScope(requiredScope))
                                                                    .RequireTenantMembership(accessLevel));

    /// <summary>Adds campaigns management Authorization.</summary>
    /// <param name="builder">Used for building policies during application startup.</param>
    public static AuthorizationPolicyBuilder RequireCampaignsManagement(this AuthorizationPolicyBuilder builder) => builder.AddRequirements(new BeCampaignsManagerRequirement());
}