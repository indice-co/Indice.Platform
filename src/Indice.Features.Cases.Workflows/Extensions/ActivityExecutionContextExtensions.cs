using System.Security.Claims;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Integration;
using Indice.Features.Cases.Workflows.Models;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Cases.Workflows.Extensions;

/// <summary>Extensions for <see cref="ActivityExecutionContext"/>.</summary>
public static class ActivityExecutionContextExtensions
{
    /// <summary>Try to get the user from the Workflow context variable "RunAsSystemUser" or from the HttpContext.</summary>
    /// <param name="context">The activity execution context.</param>
    /// <returns></returns>
    public static ClaimsPrincipal TryGetUser(this ActivityExecutionContext context) {
        var runAsSystemUser = context.GetVariable<bool>("RunAsSystemUser");
        return runAsSystemUser
            ? CasesClaimsPrincipalExtensions.SystemUser()
            : GetHttpContextUser(context);
    }

    /// <summary>Try to get the last actor from the Workflow context variable "RunAsSystemUser" or the Last Actor from the Variables.</summary>
    public static Actor TryGetLastActor(this ActivityExecutionContext context) {
        var runAsSystemUser = context.GetVariable<bool>("RunAsSystemUser");
        return runAsSystemUser
            ? Actor.Create(CasesClaimsPrincipalExtensions.SystemUser())
            : context.GetVariable<Actor>(CasesWorkflowConstants.WorkflowVariables.Actor.Current)!;
    }
    
    /// <summary>Get the HttpContext User from the <see cref="IHttpContextAccessor"/> interface.</summary>
    /// <param name="context">The activity execution context.</param>
    /// <returns></returns>
    public static ClaimsPrincipal GetHttpContextUser(this ActivityExecutionContext context) {
        var httpContextAccessor = context.GetService<IHttpContextAccessor>();
        return httpContextAccessor.HttpContext?.User!;
    }
}