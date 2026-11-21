using System.Security.Claims;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Agents.Server.Endpoints;

internal static class AgentsHandlers
{
    /// <summary>Lists the agents available to the caller, sourced from the <see cref="AgentInfoRegistry"/>. Agents that require a role the caller does not hold are left out.</summary>
    /// <param name="registry">The registry of agents registered in DI.</param>
    /// <param name="user">The caller.</param>
    public static Ok<List<AgentInfo>> Discovery(AgentInfoRegistry registry, ClaimsPrincipal user) =>
        TypedResults.Ok(registry.All(user).ToList());
}
