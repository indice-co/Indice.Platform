using Elsa.Activities.Http.Models;
using System.Text.Json.Nodes;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Models;
using System.Text.Json;

namespace Indice.Features.Cases.Workflows.Extensions;

/// <summary>Extensions for <see cref="ActivityExecutionContext"/>.</summary>
public static class ActivityExecutionContextExtensions
{
    /// <summary>Try to get the last actor either from the HTTP request body with an "actor" field
    /// or the Workflow context variable "RunAsSystemUser" or the Last Actor from the Variables.</summary>
    public static Actor TryGetLastActor(this ActivityExecutionContext context) {
        if (context.Input is HttpRequestModel { RawBody: not null } request) {
            var body = JsonNode.Parse(request.RawBody);
            var actorNode = body?["actor"];
            var tempActor = actorNode?.Deserialize<Actor>();
            if (tempActor is not null) {
                return tempActor;
            }
        }

        var runAsSystemUser = context.GetVariable<bool>("RunAsSystemUser");
        if (runAsSystemUser) {
            return Actor.Create(CasesClaimsPrincipalExtensions.SystemUser());
        }
        return context.GetVariable<Actor>(CasesWorkflowConstants.WorkflowVariables.Actor.Current)
            ?? Actor.Create(CasesClaimsPrincipalExtensions.SystemUser()); // TODO[2025-07-07]: Remove this eventually
    }
}