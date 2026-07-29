using Indice.Features.Agents.Server;
using Indice.Features.Agents.Server.Endpoints;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>HTTP surface for the caller's chat sessions: create with first question, post follow-ups, list, get, delete.</summary>
internal static class SourcesApi
{
    /// <summary>
    /// Maps the endpoints for the sources API.
    /// </summary>
    /// <param name="routes"></param>
    /// <returns></returns>
    public static RouteGroupBuilder MapSources(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<AgentsServerOptions>>().Value;
        var allowedScopes = new[] { options.ChatRequiredScope }.FilterOutNulls().ToArray();

        var group = routes.MapGroup($"{options.PathPrefix.Value?.TrimEnd('/')}/")
                          .WithName(options.GroupName)
                          .WithTags("Sources");

        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));
        group.WithOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.WithHandledException<BusinessException>()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("sources/{*path}", SourcesHandlers.GetActualSource)
             .WithName(nameof(SourcesHandlers.GetActualSource))
             .AllowAnonymous()
             .WithSummary("Retrieves the actual source document.")
             .WithDescription("Retrieves the actual source document. Used for citations and references.");

        group.MapGet("sources/{sourceId}/favicon.ico", SourcesHandlers.GetActualSourceFavicon)
             .WithName(nameof(SourcesHandlers.GetActualSourceFavicon))
             .AllowAnonymous()
             .WithSummary("Retrieves the favicon of the actual source document.")
             .WithDescription("Retrieves the favicon of the actual source document. Used for citations and references.");


        return group;
    }
}
