using Indice.Features.Cases.Workflows.Models;

namespace Indice.Features.Cases.Workflows.Integrations;

internal static class CasesManager_Extensions
{
    /// <summary>Simple mapping from Workflow <see cref="Actor"/> to <see cref="WorkflowActor"/></summary>
    public static WorkflowActor ToCasesActor(this Actor actor) =>
        new() {
            Id = actor.Id,
            Email = actor.Email,
            Name = actor.Name,
            Reference = actor.Reference
        };
}