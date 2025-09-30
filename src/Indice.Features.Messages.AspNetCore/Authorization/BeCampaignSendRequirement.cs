using Indice.Features.Messages.Core;
using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.AspNetCore.Authorization;

/// <summary>This authorization requirement specifies that an endpoint must be accessible only to Messaging tool managers.</summary>
public class BeCampaignSendRequirement : IAuthorizationRequirement
{
    /// <summary>The policy name corresponding to this requirement.</summary>
    public const string PolicyName = MessagesApi.Policies.BeCampaignManager;

    /// <summary>Creates a new instance of <see cref="BeCampaignSendRequirement"/>.</summary>
    public BeCampaignSendRequirement() {
        
    }

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(BeCampaignSendRequirement)}.";
}

/// <summary>Authorization handler corresponding to the <see cref="BeCampaignSendRequirement"/>.</summary>
public class BeCampaignSendHandler : AuthorizationHandler<BeCampaignSendRequirement>
{
    private readonly ILogger<BeCampaignSendHandler> _logger;

    /// <summary>Creates a new instance of <see cref="BeCampaignSendHandler"/>.</summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public BeCampaignSendHandler(ILogger<BeCampaignSendHandler> logger) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, BeCampaignSendRequirement requirement) {
        var userIsAnonymous = context.User?.Identity == null || !context.User.Identities.Any(identity => identity.IsAuthenticated);
        if (userIsAnonymous) {
            return Task.CompletedTask;
        }
        // Get user id/application id from the corresponding claims.
        var allowed =   context.User!.HasScope(MessagesApi.SendScope);
        // Apparently nothing else worked.
        if (allowed) {
            context.Succeed(requirement);
        } else {
            _logger.LogInformation("User {userId} does not have role {roleName}.", context.User!.FindSubjectId(), BasicRoleNames.CampaignManager);
        }
        return Task.CompletedTask;
    }
}