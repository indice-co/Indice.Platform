using Indice.Features.Cases.Core.Models.Requests;
using Indice.Features.Cases.Server;
using Indice.Features.Cases.Server.Authorization;
using Indice.Features.Cases.Server.Endpoints;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Manage check point types.</summary>
internal static class AdminCheckpointTypesApi
{
    /// <summary>Maps admin check point types endpoint.</summary>
    public static IEndpointRouteBuilder MapAdminCheckpointTypes(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;

        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage/checkpoint-types");

        group.WithTags("AdminCheckpointTypes");
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

        group.MapGet(string.Empty, AdminCheckpointTypesHandlers.GetCheckpointTypesList)
            .WithName(nameof(AdminCheckpointTypesHandlers.GetCheckpointTypesList))
            .WithSummary("Get the checkpoint types. Use the Distinct filter to get the distinct checkpoint types grouped by code.");

        group.MapGet("{checkpointTypeId:guid}", AdminCheckpointTypesHandlers.GetCheckpointTypeById)
            .WithName(nameof(AdminCheckpointTypesHandlers.GetCheckpointTypeById))
            .WithSummary("Get a specific checkpoint type by Id.")
            .RequireAuthorization(policy => policy.RequireCasesAccess(CasesAccessLevel.Administer)); // equivalent to BeCasesAdministrator

        group.MapPost(string.Empty, AdminCheckpointTypesHandlers.CreateCheckpointType)
            .WithName(nameof(AdminCheckpointTypesHandlers.CreateCheckpointType))
            .WithSummary("Create a new checkpoint type.")
            .RequireAuthorization(policy => policy.RequireCasesAccess(CasesAccessLevel.Administer)) // equivalent to BeCasesAdministrator
            .WithParameterValidation<CheckpointTypeRequest>();

        group.MapPut("{checkpointTypeId:guid}", AdminCheckpointTypesHandlers.UpdateCheckpointType)
            .WithName(nameof(AdminCheckpointTypesHandlers.UpdateCheckpointType))
            .WithSummary("Update a specific checkpoint type.")
            .RequireAuthorization(policy => policy.RequireCasesAccess(CasesAccessLevel.Administer)) // equivalent to BeCasesAdministrator
            .WithParameterValidation<CheckpointTypeRequest>();

        group.MapDelete("{checkpointTypeId:guid}", AdminCheckpointTypesHandlers.DeleteCheckpointType)
            .WithName(nameof(AdminCheckpointTypesHandlers.DeleteCheckpointType))
            .WithSummary("Delete a specific checkpoint type.")
            .RequireAuthorization(policy => policy.RequireCasesAccess(CasesAccessLevel.Administer)); // equivalent to BeCasesAdministrator

        return group;
    }
}
