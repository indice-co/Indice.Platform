using System.Net.Mime;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Server;
using Indice.Features.Cases.Server.Authorization;
using Indice.Features.Cases.Server.Endpoints;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// my Cases API
/// </summary>
internal static class MyCasesApi
{
    /// <summary>Case types from the customer's perspective.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static IEndpointRouteBuilder MapMyCases(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;

        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/my/cases");

        group.WithTags("MyCases");
        group.WithGroupName("my");

        var allowedScopes = new[] { options.RequiredScope }.FilterOutNulls().ToArray();

        // Add security requirements, all incoming requests to this API *must* be authenticated with a valid user.
        group.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("Bearer")
        ).WithHandledException<BusinessException>();

        group.AddOpenApiSecurityRequirement("oauth2", allowedScopes).WithOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet(string.Empty, MyCasesHandlers.GetMyCases)
            .WithName(nameof(MyCasesHandlers.GetMyCases))
            .WithSummary("Get the list of the customer's cases.")
            .RequireAuthorization(policy => policy.RequireCasesAccess());

        group.MapPost(string.Empty, MyCasesHandlers.CreateDraftCase)
            .WithName(nameof(MyCasesHandlers.CreateDraftCase))
            .WithSummary("Create a new draft case.")
            .WithParameterValidation<CreateDraftCaseRequest>()
            .RequireAuthorization(policy => policy.RequireCasesAccess());

        group.MapGet("{caseId}", MyCasesHandlers.GetMyCaseById)
            .WithName(nameof(MyCasesHandlers.GetMyCaseById))
            .WithSummary("Get case details by Id.")
            .RequireAuthorization(policy => policy.RequireCasesOwnershipAccess());

        group.MapPost("{caseId}/attachments", MyCasesHandlers.UploadCaseAttachment)
            .WithName(nameof(MyCasesHandlers.UploadCaseAttachment))
            .DisableAntiforgery()
            .WithSummary("Add an attachment to an existing case regardless of its status and mode (draft or not).")
            .RequireAuthorization(policy => policy.RequireCasesOwnershipAccess());

        group.MapPut("{caseId}", MyCasesHandlers.UpdateCase)
            .WithName(nameof(MyCasesHandlers.UpdateCase))
            .WithSummary("Update the case with the business data as defined at the specific case type.")
            .WithParameterValidation<UpdateCaseRequest>()
            .RequireAuthorization(policy => policy.RequireCasesOwnershipAccess());

        group.MapPost("{caseId}/submit", MyCasesHandlers.SubmitMyCase)
            .WithName(nameof(MyCasesHandlers.SubmitMyCase))
            .WithSummary("Submit the case by removing the draft mode.")
            .RequireAuthorization(policy => policy.RequireCasesOwnershipAccess());

        group.MapGet("{caseId}/download", MyCasesHandlers.DownloadMyCasePdf)
            .WithName(nameof(MyCasesHandlers.DownloadMyCasePdf))
            .WithSummary("Download case in a PDF format.")
            .Produces(StatusCodes.Status200OK, typeof(IFormFile), MediaTypeNames.Application.Pdf)
            .RequireAuthorization(policy => policy.RequireCasesOwnershipAccess());

        return routes;
    }
}
