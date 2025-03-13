using System.Net.Mime;
using Indice.AspNetCore.Configuration;
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
            .RequireCasesAccess(CasesAccessLevel.Manager)
        ).WithHandledException<BusinessException>();

        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost(string.Empty, AdminCasesHandlers.CreateDraftAdminCase)
            .WithName(nameof(AdminCasesHandlers.CreateDraftAdminCase))
            .WithSummary("Create a new case in draft mode.");

        group.MapGet("{caseId}/attachments", AdminCasesHandlers.GetCaseAttachments)
            .WithName(nameof(AdminCasesHandlers.GetCaseAttachments))
            .WithSummary("Get a list of Attachments for a CaseId");

        group.MapPost("{caseId}/attachments", AdminCasesHandlers.UploadAdminCaseAttachment)
            .WithName(nameof(AdminCasesHandlers.UploadAdminCaseAttachment))
            .DisableAntiforgery()
            .WithSummary("Add an attachment to an existing case regardless of its status and mode (draft or not).")
            .LimitUpload(uploadlimit.DefaultMaxFileSizeBytes, string.Join(',', uploadlimit.DefaultAllowedFileExtensions));

        group.MapGet("{caseId}/attachments/{attachmentId:guid}", AdminCasesHandlers.GetCaseAttachment)
            .WithName(nameof(AdminCasesHandlers.GetCaseAttachment))
            .WithSummary("Get an Case Attachment");

        group.MapGet("{caseId}/attachments/{attachmentName}", AdminCasesHandlers.GetAttachmentByField)
            .WithName(nameof(AdminCasesHandlers.GetAttachmentByField))
            .WithSummary("Get a Case Attachment by field name.");

        group.MapPut("{caseId}", AdminCasesHandlers.UpdateAdminCase)
            .WithName(nameof(AdminCasesHandlers.UpdateAdminCase))
            .WithSummary("Update the case with the business data as defined at the specific case type. This action is allowed only for draft cases.");

        group.MapPost("{caseId}/submit", AdminCasesHandlers.SubmitAdminCase)
            .WithName(nameof(AdminCasesHandlers.SubmitAdminCase))
            .WithSummary("Submit the case by removing the draft mode.");

        group.MapPatch("{caseId}/metadata", AdminCasesHandlers.PatchCaseMetadata)
            .WithName(nameof(AdminCasesHandlers.PatchCaseMetadata))
            .WithSummary("Patches the metadata of a case.");

        group.MapPost("{caseId}/comment", AdminCasesHandlers.AdminAddComment)
            .WithName(nameof(AdminCasesHandlers.AdminAddComment))
            .WithSummary("Add a comment to a case.");

        group.MapGet(string.Empty, AdminCasesHandlers.GetCases)
            .WithName(nameof(AdminCasesHandlers.GetCases))
            .WithSummary("Gets the list of all cases using the provided.");

        group.MapGet("{caseId}", AdminCasesHandlers.GetCaseById)
            .WithName(nameof(AdminCasesHandlers.GetCaseById))
            .WithSummary("Gets a case with the specified id.");

        group.MapDelete("{caseId}", AdminCasesHandlers.DeleteDraftCase)
            .WithName(nameof(AdminCasesHandlers.DeleteDraftCase))
            .WithSummary("Deletes a draft case with the specified id.");

        group.MapGet("{caseId}/timeline", AdminCasesHandlers.GetCaseTimeline)
            .WithName(nameof(AdminCasesHandlers.GetCaseTimeline))
            .WithSummary("Gets the timeline entries for a case.");

        group.MapGet("{caseId}/related-cases", AdminCasesHandlers.GetRelatedCases)
            .WithName(nameof(AdminCasesHandlers.GetRelatedCases))
            .WithSummary("Gets the cases that are related to the given id.");

        group.MapGet("{caseId}/actions", AdminCasesHandlers.GetCaseActions)
            .WithName(nameof(AdminCasesHandlers.GetCaseActions))
            .WithSummary("Gets the cases actions (Approval, edit, assignments, etc) for a case Id. Actions differ based on user role.");

        group.MapGet("{caseId}/reject-reasons", AdminCasesHandlers.GetCaseRejectReasons)
            .WithName(nameof(AdminCasesHandlers.GetCaseRejectReasons))
            .WithSummary("Get the reject reasons for a case.");

        group.MapGet("{caseId}.pdf", AdminCasesHandlers.DownloadCasePdf)
            .WithName(nameof(AdminCasesHandlers.DownloadCasePdf))
            .WithSummary("Download case in a PDF format.")
            .Produces(StatusCodes.Status200OK, typeof(IFormFile), MediaTypeNames.Application.Pdf);

        return group;
    }
}