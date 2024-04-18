using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Media.AspNetCore.Authorization;

/// <summary>This authorization requirement specifies that an endpoint must be accessible only to Midia library tool managers.</summary>
public class BeMediaLibraryManagerRequirement : IAuthorizationRequirement
{
    /// <summary>The policy name corresponding to this requirement.</summary>
    public const string PolicyName = MediaLibraryApi.Policies.BeMediaLibraryManager;

    /// <summary>Creates a new instance of <see cref="BeMediaLibraryManagerRequirement"/>.</summary>
    public BeMediaLibraryManagerRequirement() {

    }

    /// <inheritdoc/>
    public override string ToString() => nameof(BeMediaLibraryManagerRequirement);
}

/// <summary>Authorization handler corresponding to the <see cref="BeMediaLibraryManagerRequirement"/>.</summary>
public class BeMediaLibraryManagerHandler : AuthorizationHandler<BeMediaLibraryManagerRequirement>
{
    private readonly ILogger<BeMediaLibraryManagerHandler> _logger;

    /// <summary>Creates a new instance of <see cref="BeMediaLibraryManagerRequirement"/>.</summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public BeMediaLibraryManagerHandler(ILogger<BeMediaLibraryManagerHandler> logger) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, BeMediaLibraryManagerRequirement requirement) {
        var userIsAnonymous = context.User?.Identity == null || !context.User.Identities.Any(identity => identity.IsAuthenticated);
        if (userIsAnonymous) {
            return Task.CompletedTask;
        }
        // Get user id/application id from the corresponding claims.
        var allowed = context.User.IsSystemClient() || context.User.IsAdmin() || context.User.HasRoleClaim(BasicRoleNames.CampaignManager);
        // Apparently nothing else worked.
        if (allowed) {
            context.Succeed(requirement);
        } else {
            _logger.LogInformation("User {userId} does not have role {roleName}.", context.User.FindSubjectId(), BasicRoleNames.CampaignManager);
        }
        return Task.CompletedTask;
    }
}
