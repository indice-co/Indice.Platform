using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Activities.Http.Models;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Models;

namespace Indice.Features.Cases.Workflows.Extensions;

/// <summary>Extensions for <see cref="ActivityExecutionContext"/>.</summary>
public static class ActivityExecutionContextExtensions
{
    /// <summary>
    /// Try to get the last actor either either from Workflow context variable "RunAsSystemUser", or the Last Actor from the Variables.
    /// </summary>
    /// <param name="context">The activity execution context.</param>
    /// <returns>The last actor or a system user actor if none is found.</returns>
    public static Actor TryGetLastActor(this ActivityExecutionContext context)
    {
        var forceRunAsSystemUser = context.GetVariable<bool>("RunAsSystemUser");
        if (forceRunAsSystemUser)
        {
            return Actor.Create(CasesClaimsPrincipalExtensions.SystemUser());
        }
        var actor = 
        context.GetVariable<Actor>(CasesWorkflowConstants.WorkflowVariables.Actor.Current)
            ?? Actor.Create(CasesClaimsPrincipalExtensions.SystemUser()); // TODO[2025-07-07]: Remove this eventually
        return actor;
    }

    /// <summary>
    /// Attempts to set the last actor in the workflow context by extracting it from the HTTP request body.
    /// </summary>
    /// <param name="context">The activity execution context.</param>
    public static void TrySetLastActor(this ActivityExecutionContext context)
    {
        if (context.Input is HttpRequestModel { RawBody: not null } request)
        {
            var body = JsonNode.Parse(request.RawBody);
            var actorNode = body?["actor"];
            var currentActor = actorNode?.Deserialize<Actor>();
            if (currentActor is not null) {
                context.SetVariable(CasesWorkflowConstants.WorkflowVariables.Actor.Current, currentActor);
            }
        }
    }
}
