using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Server;
using Indice.Features.Agents.Server.Endpoints;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>HTTP surface for the caller's agents: discover available agents.</summary>
internal static class AgentsApi
{

    /// <summary>Maps the <c>/api/agents</c> endpoint group.</summary>
    public static RouteGroupBuilder MapAgentsDiscovery(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<AgentsServerOptions>>().Value;
        var allowedScopes = new[] { options.ChatRequiredScope }.FilterOutNulls().ToArray();

        var group = routes.MapGroup($"{options.PathPrefix.Value?.TrimEnd('/')}/agents")
                          .WithName(options.GroupName)
                          .WithTags("Agents");

        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));
        group.WithOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.WithHandledException<BusinessException>()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

        var discoveryEndpoint = group.MapGet(string.Empty, AgentsHandlers.Discovery)
             .WithName(nameof(AgentsHandlers.Discovery))
             .WithSummary("Discover agents.")
             .WithDescription("""
                List of agents available to the caller, with their metadata and capabilities. 
                The list is filtered by the caller's scopes and the agent's required scopes.
                """);
        if (options.AllowAnonymousChatCreation) {
            discoveryEndpoint.AllowAnonymous();
        }

        return group;
    }
}
