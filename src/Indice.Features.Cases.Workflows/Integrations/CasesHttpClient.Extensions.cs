using Indice.Features.Cases.Workflows.Integration;
using Indice.Features.Cases.Workflows.Models;

namespace Indice.Features.Cases.Workflows.Integrations;

internal static class CasesHttpClient_Extensions
{
    public static CasesActor ToCasesActor(this Actor actor) =>
        new() {
            Id = actor.UserId,
            Email = actor.Email,
            Name = actor.Name
        };
}