using Indice.Features.Cases.Server;
using Indice.Features.Cases.Server.Authorization;
using Indice.Features.Cases.Server.Endpoints;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Manage queries for Back-office users.</summary>
internal static class AdminQueriesApi
{
    /// <summary>Maps admin queries endpoint.</summary>
    public static IEndpointRouteBuilder MapAdminQueries(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;

        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage/queries");

        group.WithTags("AdminQueries");
        group.WithGroupName(options.GroupName);

        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).Cast<string>().ToArray();

        group.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("Bearer")
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
            .RequireCasesAccess(CasesAccessLevel.Manager)
        );
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet(string.Empty, AdminQueriesHandler.GetQueries)
             .WithName(nameof(AdminQueriesHandler.GetQueries))
             .WithSummary("Get saved queries.");

        group.MapPost(string.Empty, AdminQueriesHandler.SaveQuery)
             .WithName(nameof(AdminQueriesHandler.SaveQuery))
             .WithSummary("Save a new query.");

        group.MapDelete("{queryId}", AdminQueriesHandler.DeleteQuery)
             .WithName(nameof(AdminQueriesHandler.DeleteQuery))
             .WithSummary("Delete a query.");

        return group;
    }
}
