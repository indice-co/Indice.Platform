using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Cases.Server.Authorization;

/// <summary>
/// Represents the level of access on the Case management system
/// </summary>
public enum CasesAccessLevel
{
    /// <summary>Require meber access.</summary>
    Member = 0,
    /// <summary>
    /// Require access to manage cases.
    /// </summary>
    Manager = 1,
    /// Require access to administer cases.
    Admin = 2
}


/// <summary>This authorization requirement specifies that an endpoint must be accessible only to Messaging tool managers.</summary>
public class CasesAccessRequirement : IAuthorizationRequirement
{
    /// <summary>The policy name corresponding to this requirement.</summary>
    public const string PolicyName = CaseServerConstants.Policies.BeCasesManager;

    /// <summary>Creates a new instance of <see cref="CasesAccessRequirement"/>.</summary>
    public CasesAccessRequirement(CasesAccessLevel minimumAccessLevel = CasesAccessLevel.Member) {
        MinimumAccessLevel = minimumAccessLevel;
    }

    /// <summary>The minimum access level needed to access the protected resources</summary>
    public CasesAccessLevel MinimumAccessLevel { get; }

    /// <inheritdoc/>
    public override string ToString() => $"Requires Cases {MinimumAccessLevel} Access.";
}

/// <summary>Authorization handler corresponding to the <see cref="CasesAccessRequirement"/>.</summary>
public class CasesAccessHandler : AuthorizationHandler<CasesAccessRequirement>
{
    private readonly ILogger<CasesAccessHandler> _logger;

    /// <summary>Creates a new instance of <see cref="CasesAccessHandler"/>.</summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public CasesAccessHandler(ILogger<CasesAccessHandler> logger) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CasesAccessRequirement requirement) {
        var userIsAnonymous = context.User?.Identity == null || !context.User.Identities.Any(identity => identity.IsAuthenticated);
        if (userIsAnonymous) {
            return Task.CompletedTask;
        }
        // Get user id/application id from the corresponding claims.
        var allowed =
            context.User!.IsSystemClient() ||
            context.User!.IsAdmin() ||
            requirement.MinimumAccessLevel switch {
                CasesAccessLevel.Admin => context.User!.HasRoleClaim(BasicRoleNames.CasesAdministrator),
                CasesAccessLevel.Manager => context.User!.HasRoleClaim(BasicRoleNames.CasesAdministrator) || context.User!.HasRoleClaim(BasicRoleNames.CasesManager),
                CasesAccessLevel.Member => context.User!.HasRoleClaim(BasicRoleNames.CasesAdministrator) || context.User!.HasRoleClaim(BasicRoleNames.CasesManager) || context.User!.HasRoleClaim(BasicRoleNames.CasesUser),
                _ => false
            };

        // Apparently nothing else worked.
        if (allowed) {
            context.Succeed(requirement);
        } else {
            _logger.LogInformation("User {userId} does not have role {roleName}.", context.User!.FindSubjectId(), BasicRoleNames.CasesManager);
        }
        return Task.CompletedTask;
    }
}