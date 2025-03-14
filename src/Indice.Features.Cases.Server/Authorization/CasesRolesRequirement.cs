using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Cases.Server.Authorization;

/// <summary>
/// This authorization requirement specifies that an endpoint must be accessible only to specific roles or administrators.
/// <b> This uses the convention that if <see cref="AllowedRoles"/> is empty or has only nulls or empty strings the requirement succeeds.</b>
/// </summary>
public class CasesRolesRequirement : IAuthorizationRequirement
{
    /// <summary>The allowed role for this requirement to succeed.</summary>
    public IEnumerable<string?> AllowedRoles { get; set; }
    
    /// <summary>Creates a new instance of <see cref="CasesRolesRequirement"/>.</summary>
    public CasesRolesRequirement(IEnumerable<string?>? allowedRoles) {
        AllowedRoles = allowedRoles ?? new List<string>();
    }
}

/// <summary>Authorization handler for the <see cref="CasesRolesRequirement"/>.</summary>
public class DefaultCasesRolesHandler(ILogger<DefaultCasesRolesHandler> logger) : AuthorizationHandler<CasesRolesRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CasesRolesRequirement requirement) {
        var user = context.User;
        if (user.IsAdmin() || user.IsSystemClient()) {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (!requirement.AllowedRoles.Any()) {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (requirement.AllowedRoles.All(string.IsNullOrWhiteSpace)) {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (requirement.AllowedRoles.Any(role => user.IsInRole(role!))) {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        
        logger.LogInformation("User {userId} does not have role(s) {roleNames}.", context.User!.FindSubjectId(), string.Join(", ", requirement.AllowedRoles));
        return Task.CompletedTask;
    }
}