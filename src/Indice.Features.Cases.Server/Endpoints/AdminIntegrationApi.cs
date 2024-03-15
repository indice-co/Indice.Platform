using Indice.Features.Cases.Server.Options;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;
/// <summary>Integration endpoints with 3rd party systems.</summary>
public static class AdminIntegrationApi
{
    /// <summary>Maps admin integration endpoint.</summary>
    public static IEndpointRouteBuilder MapAdminIntegration(this IEndpointRouteBuilder routes) {
        CaseServerEndpointOptions options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerEndpointOptions>>().Value;
        var group = routes.MapGroup($"{options.ApiPrefix}/manage/integrations");
        group.WithTags("AdminIntegration");
        group.WithGroupName(ApiGroups.CasesApiGroupNamePlaceholder);
        var allowedScopes = new[] { options.ApiScope }.Where(x => x != null).Cast<string>().ToArray();
        group.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(CasesApiConstants.AuthenticationScheme)
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
        ).RequireAuthorization(CasesApiConstants.Policies.BeCasesManager);
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status400BadRequest);
        group.MapGet("customers", AdminIntegrationHandler.GetCustomers)
             .WithName(nameof(AdminIntegrationHandler.GetCustomers))
             .WithSummary("Fetch customers.");
        group.MapGet("customers/{customerId}/data/{caseTypeCode}", AdminIntegrationHandler.GetCustomerData)
             .WithName(nameof(AdminIntegrationHandler.GetCustomerData))
             .WithSummary("Fetch customer data for a specific case type code.");
        return group;
    }
}
