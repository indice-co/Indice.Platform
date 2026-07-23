using Indice.Features.Agents.Server;
using Indice.Features.Agents.Server.Endpoints;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;


internal static class SessionApi
{
    /// <summary>
    /// Maps the endpoints for the session API.
    /// </summary>
    /// <param name="routes"></param>
    /// <returns></returns>
    public static RouteGroupBuilder MapSession(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<AgentsServerOptions>>().Value;
        var allowedScopes = new[] { options.ChatRequiredScope }.FilterOutNulls().ToArray();

        var group = routes.MapGroup($"{options.PathPrefix.Value?.TrimEnd('/')}/")
                          .WithName(options.GroupName)
                          .WithTags("Session");

        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));

        group.WithOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.WithHandledException<BusinessException>()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("sessions", SessionHandlers.CreateSession)
             .WithName(nameof(SessionHandlers.CreateSession))
             .AllowAnonymous()
             .WithSummary("Create a new chat session.")
             .WithDescription("Create a new chat session with optional external ref id and referrer.");

        return group;
    }
}

