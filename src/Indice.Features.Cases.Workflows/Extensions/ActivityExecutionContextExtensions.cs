using System.Security.Claims;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Integrations;
using Indice.Features.Cases.Workflows.Models;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Cases.Workflows.Extensions;

/// <summary>Extensions for <see cref="ActivityExecutionContext"/>.</summary>
public static class ActivityExecutionContextExtensions
{
    /// <summary>Try to get the last actor from the Workflow context variable "RunAsSystemUser" or the Last Actor from the Variables.</summary>
    public static Actor TryGetLastActor(this ActivityExecutionContext context) {
        var runAsSystemUser = context.GetVariable<bool>("RunAsSystemUser");
        return runAsSystemUser
            ? Actor.Create(CasesClaimsPrincipalExtensions.SystemUser())
            : context.GetVariable<Actor>(CasesWorkflowConstants.WorkflowVariables.Actor.Current) 
              ?? Actor.Create(CasesClaimsPrincipalExtensions.SystemUser());
    }
}