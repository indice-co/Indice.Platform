using Indice.Features.Cases.Core.Models.Requests;
using Indice.Features.Cases.Server;
using Indice.Features.Cases.Server.Authorization;
using Indice.Features.Cases.Server.Endpoints;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Manage case type categories.</summary>
internal static class AdminCategoriesApi
{
    /// <summary>Maps admin categories endpoint.</summary>
    public static IEndpointRouteBuilder MapAdminCategories(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;

        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage/categories");

        group.WithTags("AdminCategories");
        group.WithGroupName(options.GroupName);

        var allowedScopes = new[] { options.RequiredScope }.FilterOutNulls().ToArray();

        group.RequireAuthorization(pb => pb
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("Bearer")
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
            .RequireCasesAccess(CasesAccessLevel.Manage)
        ).WithHandledException<BusinessException>();

        group.WithOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet(string.Empty, AdminCategoriesHandlers.GetCategoriesList)
            .WithName(nameof(AdminCategoriesHandlers.GetCategoriesList))
            .WithSummary("Get the case type categories.");

        group.MapGet("{categoryId:guid}", AdminCategoriesHandlers.GetCategoryById)
            .WithName(nameof(AdminCategoriesHandlers.GetCategoryById))
            .WithSummary("Get a specific category by Id.")
            .RequireAuthorization(policy => policy.RequireCasesAccess(CasesAccessLevel.Administer)); // equivalent to BeCasesAdministrator

        group.MapPost(string.Empty, AdminCategoriesHandlers.CreateCategory)
            .WithName(nameof(AdminCategoriesHandlers.CreateCategory))
            .WithSummary("Create a new category.")
            .RequireAuthorization(policy => policy.RequireCasesAccess(CasesAccessLevel.Administer)) // equivalent to BeCasesAdministrator
            .WithParameterValidation<CategoryRequest>();

        group.MapPut("{categoryId:guid}", AdminCategoriesHandlers.UpdateCategory)
            .WithName(nameof(AdminCategoriesHandlers.UpdateCategory))
            .WithSummary("Update a specific category.")
            .RequireAuthorization(policy => policy.RequireCasesAccess(CasesAccessLevel.Administer)) // equivalent to BeCasesAdministrator
            .WithParameterValidation<CategoryRequest>();

        group.MapDelete("{categoryId:guid}", AdminCategoriesHandlers.DeleteCategory)
            .WithName(nameof(AdminCategoriesHandlers.DeleteCategory))
            .WithSummary("Delete a specific category.")
            .RequireAuthorization(policy => policy.RequireCasesAccess(CasesAccessLevel.Administer)); // equivalent to BeCasesAdministrator

        return group;
    }
}
