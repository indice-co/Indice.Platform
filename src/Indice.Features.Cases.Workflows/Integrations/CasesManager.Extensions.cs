using Indice.Features.Cases.Workflows.Models;

namespace Indice.Features.Cases.Workflows.Integrations;

public static class CasesManager_Extensions
{
    /// <summary>Simple mapping from Workflow <see cref="Actor"/> to <see cref="UserActor"/></summary>
    public static UserActor ToCasesActor(this Actor actor) =>
        new() {
            Id = actor.Id,
            Reference = actor.Reference,
            GroupId = actor.GroupId,
            Email = actor.Email,
            Name = actor.Name,
            CurrentCulture = actor.CurrentCulture
        };
}