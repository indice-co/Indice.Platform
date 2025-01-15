using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Cases.Server.Authorization;

/// <summary>Requirement that the user must be authenticated and have a specific role.</summary>
public class AdminOrInRoleRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// The allowed role for this requirement to succeed.
    /// <remarks>If left empty or null, the requirement always succeeds.</remarks>
    /// </summary>
    public string? AllowedRole { get; set; }
    
    /// <summary>Constructor used for DI.</summary>
    public AdminOrInRoleRequirement() {}

    /// <summary>Creates a new instance of <see cref="AdminOrInRoleRequirement"/>.</summary>
    public AdminOrInRoleRequirement(string? allowedRole) {
        AllowedRole = allowedRole;
    }
}

/// <summary>Authorization handler corresponding to the <see cref="AdminOrInRoleRequirement"/>.</summary>
public class AdminOrInRoleHandler(
    AuthorizationHandlerContext context,
    AdminOrInRoleRequirement requirement,
    ILogger<AdminOrInRoleHandler> logger)
    : AuthorizationHandler<AdminOrInRoleRequirement> {
    
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminOrInRoleRequirement requirement) {
        var user = context.User;
        var userIsAnonymous = user.Identity == null || !context.User.Identities.Any(identity => identity.IsAuthenticated);
        if (userIsAnonymous) {
            return Task.CompletedTask;
        }

        if (!user.IsAdmin() &&
            !string.IsNullOrEmpty(requirement.AllowedRole) &&
            !user.IsInRole(requirement.AllowedRole)) {
            logger.LogInformation("User {userId} does not have role {roleName}.", context.User!.FindSubjectId(), requirement.AllowedRole);
            return Task.CompletedTask;
        }
        
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}