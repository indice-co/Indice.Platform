using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Cases.Server.Authorization;

public class ApprovalRequirement : IAuthorizationRequirement
{
    /// <summary>The policy name corresponding to this requirement.</summary>
    public const string PolicyName = CaseServerConstants.Policies.BeCasesApprover;
    
    public string AllowedRole { get; set; }
    
    public bool ShouldBlockPreviousUser { get; set; }

    public ApprovalRequirement() {
    }
    
    /// <summary>Creates a new instance of <see cref="CasesAccessRequirement"/>.</summary>
    public ApprovalRequirement(string allowedRole, bool shouldBlockPreviousUser) {
        AllowedRole = allowedRole;
        ShouldBlockPreviousUser = shouldBlockPreviousUser;
    }
}

/// <inheritdoc />
public class ApprovalHandler(
    ICaseApprovalService caseApprovalService,
    ILogger<ApprovalHandler> logger) : AuthorizationHandler<ApprovalRequirement>
{
    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ApprovalRequirement requirement) {
        var user = context.User;
        var allowed = true;
        
        if (context.Resource is not Guid caseId) {
            return;
        }
        
        // todo: check probably we should not allow system_client here
        if (!user.IsAdmin() &&
            !string.IsNullOrEmpty(requirement.AllowedRole) &&
            !user.IsInRole(requirement.AllowedRole)) {
            allowed = false;
        }
        
        // todo: move outside
        if (!user.IsAdmin() && requirement.ShouldBlockPreviousUser) {
            var lastApproval = await caseApprovalService.GetLastApproval(caseId);
            if (user.FindSubjectId() == lastApproval?.CreatedBy.Id) {
                allowed = false;
            }
        }
        
        if (allowed) {
            context.Succeed(requirement);
        } else {
            logger.LogInformation("User {userId} does not have sufficient access rights.", context.User!.FindSubjectId());
        }
    }
}