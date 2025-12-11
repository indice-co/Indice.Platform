using System.Net.Mime;
using System.Security.Claims;
using System.Text.Json.Nodes;
using Indice.Events;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Events;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Features.Cases.Server.Authorization;
using Indice.Features.Cases.Server.Integration;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

///<summary>Cases from the administrative perspective.</summary>
internal static class AdminCasesHandlers
{
    public static async Task<Ok<CreateCaseResponse>> CreateDraftAdminCase(
        CreateDraftCaseRequest request,
        ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions,
        IAdminCaseService adminCaseService) =>
        TypedResults.Ok(await adminCaseService.CreateDraft(currentUser.UserToActor(casesOptions.Value), request.CaseTypeCode, request.GroupId, request.Owner, request.Metadata));

    public static async Task<Ok<ResultSet<CaseAttachment>>> GetCaseAttachments(Guid caseId, IAdminCaseService adminCaseService) =>
        TypedResults.Ok(await adminCaseService.GetAttachments(caseId));

    public static async Task<Results<Ok<CasesAttachmentLink>, ValidationProblem>> UploadAdminCaseAttachment(
        Guid caseId,
        ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions,
        IFormFile file,
        IAdminCaseMessageService adminCaseMessageService,
        IOptions<CasesOptions> options) {
        if (file.Length is 0) {
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(file), "File is empty."));
        }

        var attachmentId = await adminCaseMessageService.Send(caseId, currentUser.UserToActor(casesOptions.Value), new Message {
            FileName = file.FileName,
            FileStreamAccessor = () => file.OpenReadStream()
        });
        return TypedResults.Ok(new CasesAttachmentLink {
            Id = attachmentId.GetValueOrDefault()
        });
    }

    public static async Task<Results<FileContentHttpResult, NotFound>> GetCaseAttachment(Guid caseId, Guid attachmentId, IAdminCaseService adminCaseService) {
        var attachment = await adminCaseService.GetAttachment(caseId, attachmentId);
        if (attachment is null) {
            return TypedResults.NotFound();
        }
        return TypedResults.File(attachment.Data!, attachment.ContentType, attachment.FileName);
    }

    public static async Task<Results<FileContentHttpResult, NotFound>> GetAttachmentByField(Guid caseId, string fieldName,
        ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions,
        IAdminCaseService adminCaseService) {
        var attachment = await adminCaseService.GetAttachmentByField(currentUser.UserToActor(casesOptions.Value), caseId, fieldName);
        if (attachment is null) {
            return TypedResults.NotFound();
        }
        return TypedResults.File(attachment.Data!, attachment.ContentType, attachment.FileName);
    }

    public static async Task<Results<NoContent, NotFound>> UpdateAdminCase(Guid caseId, UpdateCaseRequest request,
        IAdminCaseService adminCaseService,
        ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions) {
        await adminCaseService.UpdateData(currentUser.UserToActor(casesOptions.Value), caseId, request.Data);
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, NotFound>> SubmitAdminCase(Guid caseId, JsonNode data, IAdminCaseService adminCaseService,
        ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions) {
        await adminCaseService.UpdateData(currentUser.UserToActor(casesOptions.Value), caseId, data);
        await adminCaseService.PublishData(caseId); // todo: replace with the above (saveData with publish flag and SaveAndPublish expressive interface method)
        await adminCaseService.Submit(currentUser.UserToActor(casesOptions.Value), caseId);
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, NotFound>> PatchCaseMetadata(Guid caseId,
        Dictionary<string, string> metadata,
        IAdminCaseService adminCaseService
        ) {
        var result = await adminCaseService.PatchCaseMetadata(caseId, metadata);
        if (!result) {
            return TypedResults.NotFound();
        }
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> AdminAddComment(Guid caseId, SendCommentRequest request,
        IAdminCaseMessageService adminCaseMessageService,
        ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions) {
        _ = await adminCaseMessageService.Send(caseId, currentUser.UserToActor(casesOptions.Value), new Message {
            Comment = request.Comment,
            PrivateComment = request.PrivateComment,
            ReplyToCommentId = request.ReplyToCommentId
        });
        return TypedResults.NoContent();
    }

    public static async Task<Ok<ResultSet<CasePartial>>> GetCases([AsParameters] ListOptions options, [AsParameters] GetCasesListFilter filter,
        IAdminCaseService adminCaseService,
        ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions) =>
        TypedResults.Ok(await adminCaseService.GetCases(currentUser.UserToActor(casesOptions.Value), ListOptions.Create(options, filter)));

    public static async Task<Results<Ok<Case>, NotFound>> GetCaseById(Guid caseId, IAdminCaseService adminCareService, bool fetchPublicData = false) {
        var @case = await adminCareService.GetCaseById(caseId, fetchPublicData, false);
        return @case is not null ? TypedResults.Ok(@case) : TypedResults.NotFound();
    }

    public static async Task<Results<NoContent, NotFound>> DeleteDraftCase(Guid caseId, IAdminCaseService adminCaseService, ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions) {
        await adminCaseService.DeleteDraft(currentUser.UserToActor(casesOptions.Value), caseId);
        return TypedResults.NoContent();
    }

    public static async Task<Ok<List<TimelineEntry>>> GetCaseTimeline(Guid caseId, IAdminCaseService adminCaseService) {
        var timeline = await adminCaseService.GetTimeline(caseId);
        return TypedResults.Ok(timeline);
    }

    public static async Task<Ok<List<CasePartial>>> GetRelatedCases(Guid caseId, IAdminCaseService adminCaseService, ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions) {
        var cases = await adminCaseService.GetRelatedCases(currentUser.UserToActor(casesOptions.Value), caseId);
        return TypedResults.Ok(cases);
    }

    public static async Task<Results<Ok<CaseActions>, NotFound>> GetCaseActions(
        Guid caseId,
        ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions,
        ICasesWorkflowManager workflowManager,
        ICaseActionsService caseBookmarkService,
        IAdminCaseService adminCaseService,
        ICaseApprovalService caseApprovalService,
        IAuthorizationService authorizationService,
        CasesDbContext dbContext
    ) {
        // If user has no role, do not allow any actions
        if (!currentUser.FindAll(c => c.Type == BasicClaimTypes.Role).Any() && !currentUser.IsSystemClient()) {
            return TypedResults.Ok(new CaseActions());
        }

        var @case = await dbContext.Cases.Where(x => x.Id == caseId)
            .Select(x => new {
                x.Id,
                AssignedToId = x.AssignedTo == null ? null : x.AssignedTo.Id
            })
            .FirstOrDefaultAsync();

        if (@case == null) {
            return TypedResults.NotFound();
        }

        // Get List of Available Actions from Workflow
        var actions = await workflowManager.GetActionsByCaseId(caseId) as AvailableActions;
        if (actions is null) {
            return TypedResults.Ok(new CaseActions());
        }

        var assignedToId = @case!.AssignedToId;
        var caseIsAssigned = !string.IsNullOrWhiteSpace(assignedToId);

        // If user is Admin, they can do everything except assign an already assigned case
        if (currentUser.IsAdmin() || currentUser.IsSystemClient()) {
            return TypedResults.Ok(new CaseActions {
                HasAssignment = (actions.AssignmentBookmarks?.Count > 0) && !caseIsAssigned,
                HasApproval = actions.ApprovalBookmarks?.Count > 0,
                HasUnassignment = caseIsAssigned,
                HasEdit = actions.EditBookmarks?.Count > 0,
                CustomActions = actions.CustomActions?.Select(x => x.CreateFromWorkflowAction()).ToList()!
            });
        }

        var hasApproval = false;
        var hasAssignment = false;
        var hasEdit = false;
        var hasCustom = false;
        var isAssignedToCurrentUser = caseIsAssigned && assignedToId == currentUser.FindSubjectId();

        // For Assignment Action:
        // 1. User must have the specified role
        // 2. Case must not be already assigned
        if (actions.AssignmentBookmarks is { Count: > 0 }) {
            var authorizationResult = await authorizationService.AuthorizeAsync(currentUser, caseId, new CasesRolesRequirement([actions.AssignmentBookmarks.FirstOrDefault()?.Role]));
            if (authorizationResult.Succeeded && !caseIsAssigned) {
                hasAssignment = true;
            }
        }

        // For Approval Action:
        if (actions.ApprovalBookmarks is { Count: > 0 }) {
            var authorizationResult = await authorizationService.AuthorizeAsync(currentUser, caseId, new CasesRolesRequirement([actions.ApprovalBookmarks.FirstOrDefault()?.Role]));
            // 1. User must have the specified role
            if (authorizationResult.Succeeded) {
                hasApproval = true;
            }

            // 2. Case must be assigned to them if already assigned
            if (caseIsAssigned && !isAssignedToCurrentUser) {
                hasApproval = false;
            }

            // 3. If BlockPreviousApprover is set, they must not be the previous approver
            if (actions.ApprovalBookmarks.First().BlockPreviousApprover) {
                var lastApproval = await caseApprovalService.GetLastApproval(caseId);
                if (currentUser.FindSubjectId() == lastApproval?.CreatedBy.Id) {
                    hasApproval = false;
                }
            }
        }

        // For Edit Action:
        // 1. User must have the specified role
        // 2. Case must be assigned to them
        if (actions.EditBookmarks is { Count: > 0 }) {
            var authorizationResult = await authorizationService.AuthorizeAsync(currentUser, caseId, new CasesRolesRequirement([actions.EditBookmarks.FirstOrDefault()?.Role]));
            if (authorizationResult.Succeeded && isAssignedToCurrentUser) {
                hasEdit = true;
            }
        }


        // For Custom Action:
        // User must have the specified role
        if (actions.CustomActions is { Count: > 0 }) {
            var authorizationResult = await authorizationService.AuthorizeAsync(currentUser, caseId, new CasesRolesRequirement([actions.CustomActions.FirstOrDefault()?.AllowedRole]));
            if (authorizationResult.Succeeded) {
                hasCustom = true;
            }
        }

        return TypedResults.Ok(new CaseActions {
            HasApproval = hasApproval,
            HasAssignment = hasAssignment,
            HasEdit = hasEdit,
            CustomActions = hasCustom ? actions.CustomActions?.Select(x => x.CreateFromWorkflowAction()).ToList()! : []
        });
    }

    public static async Task<Ok<List<RejectReason>>> GetCaseRejectReasons(Guid caseId, ICaseApprovalService caseApprovalService,
        ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions) =>
        TypedResults.Ok(await caseApprovalService.GetRejectReasons(currentUser.UserToActor(casesOptions.Value), caseId));

    public static async Task<Results<FileContentHttpResult, NotFound>> DownloadCasePdf(Guid caseId,
        ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions,
        IAdminCaseService adminCaseService,
        IPlatformEventService platformEventService,
        ICaseTemplateService caseTemplateService,
        ICasePdfService casePdfService) {
        var @case = await adminCaseService.GetCaseById(caseId, false, true);
        if (@case is null) {
            return TypedResults.NotFound();
        }
        var file = await CreatePdf(@case, caseTemplateService, casePdfService);
        var fileName = $"{@case.CaseType.Code}-{DateTimeOffset.UtcNow.Date:dd-MM-yyyy}.pdf";
        await platformEventService.Publish(new CaseDownloadedEvent(@case, CasesCoreConstants.Channels.Agent));
        return TypedResults.File(file, MediaTypeNames.Application.Pdf, fileName);
    }

    private static async Task<byte[]> CreatePdf(Case @case, ICaseTemplateService caseTemplateService, ICasePdfService casePdfService) {
        var template = await caseTemplateService.RenderTemplateAsync(@case);
        var pdfOptions = new PdfOptions(@case.CaseType.Config);
        return await casePdfService.HtmlToPdfAsync(template, pdfOptions, @case);
    }

    /// <summary>Publish the latest version of Data.</summary>
    /// <param name="caseId"></param>
    /// <param name="adminCaseService"></param>
    public static async Task PublishCasePrivateData(Guid caseId, IAdminCaseService adminCaseService)
        => await adminCaseService.PublishData(caseId);

    public static async Task<Results<Ok<JsonNode>, NotFound>> InitializeCaseData(
        Guid caseId, IAdminCaseService adminCaseService, ICaseDataInitializer caseDataInitializer, IOptions<CasesOptions> casesOptions, ClaimsPrincipal currentUser) {
        var @case = await adminCaseService.GetCaseById(caseId, fetchPublicData: false, includeAttachmentData: false);
        if (@case is null) {
            return TypedResults.NotFound();
        }

        var owner = new Contact() {
            Reference = @case.OwnerId,
            Tin = @case.OwnerTin
        };

        var data = await caseDataInitializer.InitializeAsync(currentUser.UserToActor(casesOptions.Value), @case.CaseType.Code.ToString(), owner);
        return data is null
            ? TypedResults.Ok(JsonNode.Parse("{}"))
            : TypedResults.Ok(data);
    }
}
