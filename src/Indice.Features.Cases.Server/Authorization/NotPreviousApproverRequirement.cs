using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Cases.Server.Authorization;

public class NotPreviousApproverRequirement : IAuthorizationRequirement
{
    public bool ShouldBlockPreviousUser { get; set; }
    
    public NotPreviousApproverRequirement() {}

    public NotPreviousApproverRequirement(bool shouldBlockPreviousUser) {
        ShouldBlockPreviousUser = shouldBlockPreviousUser;
    }
}

public class NotPreviousApproverRequirementHandler(
    ICaseApprovalService caseApprovalService,
    ILogger<NotPreviousApproverRequirementHandler> logger) : AuthorizationHandler<NotPreviousApproverRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, NotPreviousApproverRequirement requirement) {
        var user = context.User;
        var allowed = true;
        
        if (context.Resource is not Guid caseId) {
            return;
        }
        
        if (requirement.ShouldBlockPreviousUser) {
            var lastApproval = await caseApprovalService.GetLastApproval(caseId);
            if (user.FindSubjectId() == lastApproval?.CreatedBy.Id) {
                logger.LogInformation("User {userId} is previous Approver.", context.User!.FindSubjectId());
                return;
            }
        }
        
        context.Succeed(requirement);
    }
}