using Indice.AspNetCore.Configuration;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

/// /// <summary>Cases from the administrative perspective.</summary>
public static class AdminCasesApi
{
    /// <summary>Maps admin cases endpoint.</summary>
    public static IEndpointRouteBuilder MapAdminCases(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;
        var uploadlimit = routes.ServiceProvider.GetRequiredService<IOptions<LimitUploadOptions>>().Value;
        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage/cases");

        group.WithTags("AdminCases");
        group.WithGroupName(options.GroupName);

        var allowedScopes = new[] {
            options.RequiredScope
        }.Where(x => x != null).ToArray();
        group.RequireAuthorization(policy => policy.RequireClaim(BasicClaimTypes.Scope, allowedScopes))
            .RequireAuthorization(CasesApiConstants.Policies.BeCasesManager);

        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost(string.Empty, AdminCasesHandler.CreateDraftAdminCase)
            .WithName(nameof(AdminCasesHandler.CreateDraftAdminCase))
            .WithSummary("Create a new case in draft mode.");

        group.MapGet("{caseId:guid}/attachments", AdminCasesHandler.GetCaseAttachments)
            .WithName(nameof(AdminCasesHandler.GetCaseAttachments))
            .WithSummary("Get a list of Attachments for a CaseId");

        group.MapPost("{caseId:guid}/attachments", AdminCasesHandler.UploadAdminCaseAttachment)
            .WithName(nameof(AdminCasesHandler.UploadAdminCaseAttachment))
            .DisableAntiforgery()
            .WithSummary("Add an attachment to an existing case regardless of its status and mode (draft or not).")
            .LimitUpload(uploadlimit.DefaultMaxFileSizeBytes, string.Join(',', uploadlimit.DefaultAllowedFileExtensions));

        group.MapGet("{caseId:guid}/attachments/{attachmentId:guid}", AdminCasesHandler.GetCaseAttachment)
            .WithName(nameof(AdminCasesHandler.GetCaseAttachment))
            .WithSummary("Get an Case Attachment");

        group.MapGet("{caseId:guid}/attachments/{attachmentName}", AdminCasesHandler.GetAttachmentByField)
            .WithName(nameof(AdminCasesHandler.GetAttachmentByField))
            .WithSummary("Get a Case Attachment by field name.");

        group.MapPut("{caseId:guid}", AdminCasesHandler.UpdateAdminCase)
            .WithName(nameof(AdminCasesHandler.UpdateAdminCase))
            .WithSummary("Update the case with the business data as defined at the specific case type. This action is allowed only for draft cases.");

        group.MapPost("{caseId:guid}/submit", AdminCasesHandler.SubmitAdminCase)
            .WithName(nameof(AdminCasesHandler.SubmitAdminCase))
            .WithSummary("Submit the case by removing the draft mode.");

        group.MapPatch("{caseId:guid}/metadata", AdminCasesHandler.PatchCaseMetadata)
            .WithName(nameof(AdminCasesHandler.PatchCaseMetadata))
            .WithSummary("Patches the metadata of a case.");

        group.MapPost("{caseId:guid}/comment", AdminCasesHandler.AdminAddComment)
            .WithName(nameof(AdminCasesHandler.AdminAddComment))
            .WithSummary("Add a comment to a case.");

        group.MapGet(string.Empty, AdminCasesHandler.GetCases)
            .WithName(nameof(AdminCasesHandler.GetCases))
            .WithSummary("Gets the list of all cases using the provided.");

        group.MapGet("{caseId:guid}", AdminCasesHandler.GetCaseById)
            .WithName(nameof(AdminCasesHandler.GetCaseById))
            .WithSummary("Gets a case with the specified id.");

        group.MapDelete("{caseId:guid}", AdminCasesHandler.DeleteDraftCase)
            .WithName(nameof(AdminCasesHandler.DeleteDraftCase))
            .WithSummary("Deletes a draft case with the specified id.");

        group.MapGet("{caseId:guid}/timeline", AdminCasesHandler.GetCaseTimeline)
            .WithName(nameof(AdminCasesHandler.GetCaseTimeline))
            .WithSummary("Gets the timeline entries for a case.");

        group.MapGet("{caseId:guid}/related-cases", AdminCasesHandler.GetRelatedCases)
            .WithName(nameof(AdminCasesHandler.GetRelatedCases))
            .WithSummary("Gets the cases that are related to the given id.");

        group.MapGet("{caseId:guid}/actions", AdminCasesHandler.GetCaseActions)
            .WithName(nameof(AdminCasesHandler.GetCaseActions))
            .WithSummary("Gets the cases actions (Approval, edit, assignments, etc) for a case Id. Actions differ based on user role.");

        group.MapGet("{caseId:guid}/reject-reasons", AdminCasesHandler.GetCaseRejectReasons)
            .WithName(nameof(AdminCasesHandler.GetCaseRejectReasons))
            .WithSummary("Get the reject reasons for a case.");

        group.MapGet("{caseId:guid}.pdf", AdminCasesHandler.DownloadCasePdf)
            .WithName(nameof(AdminCasesHandler.DownloadCasePdf))
            .WithSummary("Download case in a PDF format.");
        return group;
    }
}
