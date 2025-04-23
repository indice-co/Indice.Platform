using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Cases.Server.Authorization;

/// <summary>Authorization handler corresponding to the <see cref="CasesSystemAccessRequirement"/>.</summary>
public class CasesAccessRoleBasedHandler : AuthorizationHandler<CasesSystemAccessRequirement>
{
    private readonly ILogger<CasesAccessRoleBasedHandler> _logger;

    /// <summary>Creates a new instance of <see cref="CasesAccessRoleBasedHandler"/>.</summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public CasesAccessRoleBasedHandler(ILogger<CasesAccessRoleBasedHandler> logger) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CasesSystemAccessRequirement requirement) {
        var userIsAnonymous = context.User?.Identity == null || !context.User.Identities.Any(identity => identity.IsAuthenticated);
        if (userIsAnonymous) {
            return Task.CompletedTask;
        }
        // Get user id/application id from the corresponding claims.
        var allowed =
            context.User!.IsSystemClient() ||
            context.User!.IsAdmin() ||
            requirement.MinimumAccessLevel switch {
                CasesAccessLevel.Administer => context.User!.HasRoleClaim(BasicRoleNames.CasesAdministrator),
                CasesAccessLevel.Manage => context.User!.HasRoleClaim(BasicRoleNames.CasesAdministrator) || context.User!.HasRoleClaim(BasicRoleNames.CasesManager),
                CasesAccessLevel.Read => context.User!.HasRoleClaim(BasicRoleNames.CasesAdministrator) || context.User!.HasRoleClaim(BasicRoleNames.CasesManager) || context.User!.HasRoleClaim(BasicRoleNames.CasesUser),
                _ => false
            };

        if (allowed) {
            context.Succeed(requirement);
        } else {
            _logger.LogInformation("User {UserId} does not have role {RoleName}.", context.User!.FindSubjectId(), BasicRoleNames.CasesManager);
        }
        return Task.CompletedTask;
    }
}
