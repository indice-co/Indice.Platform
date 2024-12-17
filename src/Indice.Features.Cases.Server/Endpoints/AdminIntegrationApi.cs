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
/// <summary>Integration endpoints with 3rd party systems.</summary>
internal static class AdminIntegrationApi
{
    /// <summary>Maps admin integration endpoint.</summary>
    public static IEndpointRouteBuilder MapAdminIntegration(this IEndpointRouteBuilder routes) {

        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;

        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage/integrations");
        group.WithTags("AdminIntegration");
        group.WithGroupName(options.GroupName);

        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).Cast<string>().ToArray();
        group.RequireAuthorization(pb => pb
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("Bearer")
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
            .RequireCasesAccess(CasesAccessLevel.Manager)
        );
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("customers", AdminIntegrationHandler.GetCustomers)
             .WithName(nameof(AdminIntegrationHandler.GetCustomers))
             .WithSummary("Fetch customers.");

        group.MapGet("customers/{customerId}/data/{caseTypeCode}", AdminIntegrationHandler.GetCustomerData)
             .WithName(nameof(AdminIntegrationHandler.GetCustomerData))
             .WithSummary("Fetch customer data for a specific case type code.");

        return group;
    }
}
