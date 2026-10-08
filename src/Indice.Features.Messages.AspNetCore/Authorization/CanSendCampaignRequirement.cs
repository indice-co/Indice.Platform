using Indice.Features.Messages.Core;
using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.AspNetCore.Authorization;

/// <summary>This authorization requirement specifies that an endpoint must be accessible only to Messaging tool managers.</summary>
public class CanSendCampaignRequirement : IAuthorizationRequirement
{

    /// <summary>Creates a new instance of <see cref="CanSendCampaignRequirement"/>.</summary>
    public CanSendCampaignRequirement() {
        
    }

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(CanSendCampaignRequirement)}.";
}

/// <summary>Authorization handler corresponding to the <see cref="CanSendCampaignRequirement"/>.</summary>
public class CanSendCampaignHandler : AuthorizationHandler<CanSendCampaignRequirement>
{
    private readonly ILogger<CanSendCampaignHandler> _logger;

    /// <summary>Creates a new instance of <see cref="CanSendCampaignHandler"/>.</summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public CanSendCampaignHandler(ILogger<CanSendCampaignHandler> logger) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CanSendCampaignRequirement requirement) {
        var userIsAnonymous = context.User?.Identity == null || !context.User.Identities.Any(identity => identity.IsAuthenticated);
        if (userIsAnonymous) {
            return Task.CompletedTask;
        }
        // Get user id/application id from the corresponding claims.
        var allowed = HasSendScope(context) || 
                        (HasMessagingScope(context) && IsUserCampaignManager(context));
        // Apparently nothing else worked.
        if (allowed) {
            context.Succeed(requirement);
        } else {
            _logger.LogInformation("User {userId} does not have role {roleName}.", context.User!.FindSubjectId(), BasicRoleNames.CampaignManager);
        }
        return Task.CompletedTask;
    }

    private static bool IsUserCampaignManager(AuthorizationHandlerContext context) {
        return context.User!.IsAdmin() || context.User!.HasRoleClaim(BasicRoleNames.CampaignManager);
    }

    private static bool HasMessagingScope(AuthorizationHandlerContext context) {
        //this is for clients credentials flow
        return context.User!.HasScope(MessagesApi.Scope);
    }
    private static bool HasSendScope(AuthorizationHandlerContext context) {
        //this is for clients credentials flow
        return context.User!.HasScope(MessagesApi.SendScope);
    }
}