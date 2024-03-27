using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indice.Features.Messages.Core;
using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.AspNetCore.Authorization;

/// <summary>This authorization requirement specifies that an endpoint must be accessible only to Messaging tool managers.</summary>
public class BeCampaignsManagerRequirement : IAuthorizationRequirement
{
    /// <summary>The policy name corresponding to this requirement.</summary>
    public const string PolicyName = "BeCampaignsManager";

    /// <summary>Creates a new instance of <see cref="BeCampaignsManagerRequirement"/>.</summary>
    public BeCampaignsManagerRequirement() {
        
    }

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(BeCampaignsManagerRequirement)}.";
}

/// <summary>Authorization handler corresponding to the <see cref="BeCampaignsManagerRequirement"/>.</summary>
public class BeCampaignsManagerHandler : AuthorizationHandler<BeCampaignsManagerRequirement>
{
    private readonly ILogger<BeCampaignsManagerHandler> _logger;

    /// <summary>Creates a new instance of <see cref="BeCampaignsManagerHandler"/>.</summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <param name="apiOptions"></param>
    public BeCampaignsManagerHandler(ILogger<BeCampaignsManagerHandler> logger, IOptions<MessageManagementOptions> apiOptions) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, BeCampaignsManagerRequirement requirement) {
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