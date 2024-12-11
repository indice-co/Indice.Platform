using Indice.Features.Cases.Server;
using Indice.Features.Cases.Server.Endpoints;
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

        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).Cast<string>().ToArray();

        // Add security requirements, all incoming requests to this API *must* be authenticated with a valid user.
        group.RequireAuthorization(policy => policy
             .RequireCasesAccess()
        ).RequireAuthorization(CasesApiConstants.Policies.BeCasesUser);

        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet(string.Empty, MyCasesHandler.GetMyCases)
             .WithName(nameof(MyCasesHandler.GetMyCases))
             .WithSummary("Get the list of the customer's cases.");

        group.MapGet("{caseId:guid}", MyCasesHandler.GetMyCaseById)
             .WithName(nameof(MyCasesHandler.GetMyCaseById))
             .WithSummary("Get case details by Id.");

        group.MapPost(string.Empty, MyCasesHandler.CreateDraftCase)
             .WithName(nameof(MyCasesHandler.CreateDraftCase))
             .WithSummary("Create a new draft case.");

        group.MapPost("{caseId:guid}/attachments", MyCasesHandler.UploadCaseAttachment)
             .WithName(nameof(MyCasesHandler.UploadCaseAttachment))
             .WithSummary("Add an attachment to an existing case regardless of its status and mode (draft or not).");

        group.MapPut("{caseId:guid}", MyCasesHandler.UpdateCase)
             .WithName(nameof(MyCasesHandler.UpdateCase))
             .WithSummary("Update the case with the business data as defined at the specific case type.");

        group.MapPost("{caseId:guid}/submit", MyCasesHandler.SubmitMyCase)
                .WithName(nameof(MyCasesHandler.SubmitMyCase))
                .WithSummary("Submit the case by removing the draft mode.");

        group.MapGet("{caseId:guid}/download", MyCasesHandler.DownloadMyCasePdf)
                .WithName(nameof(MyCasesHandler.DownloadMyCasePdf))
                .WithSummary("Download case in a PDF format.");

        return routes;
    }
}
