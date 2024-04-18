using Indice.Features.Messages.Core;
using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.AspNetCore.Authorization;

/// <summary>This authorization requirement specifies that an endpoint must be accessible only to Messaging tool managers.</summary>
public class BeCampaignManagerRequirement : IAuthorizationRequirement
{
    /// <summary>The policy name corresponding to this requirement.</summary>
    public const string PolicyName = MessagesApi.Policies.BeCampaignManager;

    /// <summary>Creates a new instance of <see cref="BeCampaignManagerRequirement"/>.</summary>
    public BeCampaignManagerRequirement() {
        
    }

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(BeCampaignManagerRequirement)}.";
}

/// <summary>Authorization handler corresponding to the <see cref="BeCampaignManagerRequirement"/>.</summary>
public class BeCampaignManagerHandler : AuthorizationHandler<BeCampaignManagerRequirement>
{
    private readonly ILogger<BeCampaignManagerHandler> _logger;

    /// <summary>Creates a new instance of <see cref="BeCampaignManagerHandler"/>.</summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public BeCampaignManagerHandler(ILogger<BeCampaignManagerHandler> logger) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, BeCampaignManagerRequirement requirement) {
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