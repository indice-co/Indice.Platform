using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Cases.Server.Authorization;

public class NotSpecificUserRequirement : IAuthorizationRequirement
{
    public bool ShouldBlockPreviousUser { get; set; }
    
    public NotSpecificUserRequirement(bool shouldBlockPreviousUser) {
        ShouldBlockPreviousUser = shouldBlockPreviousUser;
    }
}

public class NotSpecificUserHandler(
    ICaseApprovalService caseApprovalService,
    ILogger<NotSpecificUserHandler> logger) : AuthorizationHandler<NotSpecificUserRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, NotSpecificUserRequirement requirement) {
        var user = context.User;
        
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