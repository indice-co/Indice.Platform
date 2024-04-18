using Indice.Features.Media.AspNetCore;
using Indice.Features.Media.AspNetCore.Authorization;
using Indice.Security;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// Extensions on the <see cref="AuthorizationPolicyBuilder"/> and <see cref="AuthorizationOptions"/>.
/// </summary>
public static class MediaLibraryAuthorizationExtensions
{
    /// <summary>Adds tenant member authorization policy according to access level.</summary>
    /// <param name="options">Provides programmatic configuration used by <see cref="IAuthorizationService"/> and <see cref="IAuthorizationPolicyProvider"/>.</param>
    /// <param name="requiredScope">The scope required to operate. Defaults to <strong>messages</strong></param>
    public static void AddMediaLibraryManagementPolicy(this AuthorizationOptions options, string requiredScope = MediaLibraryApi.Scope) =>
        options.AddPolicy(BeMediaLibraryManagerRequirement.PolicyName, policyBuilder => policyBuilder
                                                                    .AddAuthenticationSchemes(MediaLibraryApi.AuthenticationScheme)
                                                                    .RequireAuthenticatedUser()
                                                                    .RequireAssertion(x => x.User.HasScope(requiredScope))
                                                                    .RequireMediaManagement());

    
    /// <summary>Adds campaigns management Authorization.</summary>
    /// <param name="builder">Used for building policies during application startup.</param>
    public static AuthorizationPolicyBuilder RequireMediaManagement(this AuthorizationPolicyBuilder builder) => builder.AddRequirements(new BeMediaLibraryManagerRequirement());
}