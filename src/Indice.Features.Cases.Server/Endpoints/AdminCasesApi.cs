using Indice.AspNetCore.Configuration;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

/// /// <summary>Cases from the administrative perspective.</summary>
internal static class AdminCasesApi
{
    /// <summary>Maps admin cases endpoint.</summary>
    public static IEndpointRouteBuilder MapAdminCases(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;
        var uploadlimit = routes.ServiceProvider.GetRequiredService<IOptions<LimitUploadOptions>>().Value;
        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage/cases");

        group.WithTags("AdminCases");
        group.WithGroupName(options.GroupName);
        
        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).Cast<string>().ToArray();
        group.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("Bearer")
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
            .RequireCasesAccess(Authorization.CasesAccessLevel.Manager) // equivalent to BeCasesManager
        );

        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden);
        
        group.MapPost("", AdminCasesHandler.CreateDraftAdminCase)
             .WithName(nameof(AdminCasesHandler.CreateDraftAdminCase))
             .WithSummary("Create a new case in draft mode.");

        group.MapGet("{caseId}/attachments",AdminCasesHandler.GetCaseAttachments)
            .WithName(nameof(AdminCasesHandler.GetCaseAttachments))
            .WithSummary("Get a list of Attachments for a CaseId");
        
        group.MapPost("{caseId}/attachments", AdminCasesHandler.UploadAdminCaseAttachment)
            .WithName(nameof(AdminCasesHandler.UploadAdminCaseAttachment))
            .WithSummary("Add an attachment to an existing case regardless of its status and mode (draft or not).")
            .LimitUpload(uploadlimit.DefaultMaxFileSizeBytes, string.Join(',', uploadlimit.DefaultAllowedFileExtensions));

        group.MapGet("{caseId}/attachments/{attachmentId}", AdminCasesHandler.GetCaseAttachment)
            .WithName(nameof(AdminCasesHandler.GetCaseAttachment))
            .WithSummary("Get an Case Attachment"); ;
        
        group.MapPut("{caseId}", AdminCasesHandler.UpdateAdminCase)
            .WithName(nameof(AdminCasesHandler.UpdateAdminCase))
            .WithSummary("Update the case with the business data as defined at the specific case type. This action is allowed only for draft cases.");
        
        group.MapPost("{caseId}/submit", AdminCasesHandler.SubmitAdminCase)
            .WithName(nameof(AdminCasesHandler.SubmitAdminCase))
            .WithSummary("Submit the case by removing the draft mode.");
        
        group.MapGet("", AdminCasesHandler.GetCases)
            .WithName(nameof(AdminCasesHandler.GetCases))
            .WithSummary("Gets the list of all cases using the provided.");
        
        group.MapGet("{caseId}", AdminCasesHandler.GetCaseById)
            .WithName(nameof(AdminCasesHandler.GetCaseById))
            .WithSummary("Gets a case with the specified id.");
        
        group.MapDelete("{caseId}", AdminCasesHandler.DeleteDraftCase)
            .WithName(nameof(AdminCasesHandler.DeleteDraftCase))
            .WithSummary("Deletes a draft case with the specified id.");
        
        group.MapGet("{caseId}/timeline", AdminCasesHandler.GetCaseTimeline)
            .WithName(nameof(AdminCasesHandler.GetCaseTimeline))
            .WithSummary("Gets the timeline entries for a case.");
        
        group.MapGet("{caseId}/actions", AdminCasesHandler.GetCaseActions)
            .WithName(nameof(AdminCasesHandler.GetCaseActions))
            .WithSummary("Gets the cases actions (Approval, edit, assignments, etc) for a case Id. Actions differ based on user role.");
        
        group.MapGet("{caseId}/reject-reasons", AdminCasesHandler.GetCaseRejectReasons)
            .WithName(nameof(AdminCasesHandler.GetCaseRejectReasons))
            .WithSummary("Get the reject reasons for a case.");

        group.MapGet("{caseId}.pdf", AdminCasesHandler.DownloadCasePdf)
            .WithName(nameof(AdminCasesHandler.DownloadCasePdf))
            .WithSummary("Download case in a PDF format.");
        return group;
    }
}
