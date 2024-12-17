using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

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

        group.RequireAuthorization(pb => pb
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("Bearer")
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
            .RequireCasesAccess(Authorization.CasesAccessLevel.Manager)
        ).RequireAuthorization(CasesApiConstants.Policies.BeCasesManager);
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

        group.MapDelete("{queryId:guid}", AdminQueriesHandler.DeleteQuery)
             .WithName(nameof(AdminQueriesHandler.DeleteQuery))
             .WithSummary("Delete a query.");

        return group;
    }
}
