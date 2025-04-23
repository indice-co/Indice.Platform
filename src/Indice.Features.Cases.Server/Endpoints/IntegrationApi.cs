using Indice.AspNetCore.Configuration;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

internal static class IntegrationApi
{
    public static IEndpointRouteBuilder MapIntegration(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;
        var uploadlimit = routes.ServiceProvider.GetRequiredService<IOptions<LimitUploadOptions>>().Value;
        
        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage/workflow-integration");
        group.WithGroupName("cases-workflow");
        group.WithTags("Cases workflow");
        
        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).Cast<string>().ToArray();
        group.RequireAuthorization(policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(ctx=> ctx.User.IsSystemClient() || ctx.User.IsAdmin())
            ).WithHandledException<Exception>();
        
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Product Endpoints - Synchronous response
        group.MapGet("{caseId}", IntegrationHandlers.GetById)
            .WithName(nameof(IntegrationHandlers.GetById))
            .WithSummary("Gets an admin case.");
        
        group.MapGet("{caseId}/last-approval", IntegrationHandlers.GetLastApproval)
            .WithName(nameof(IntegrationHandlers.GetLastApproval))
            .WithSummary("Gets last approval of a case.");
        
        // Product Endpoints - Notifications but current or further activities down the line depend on completion of action
        group.MapPost("{caseId}/send-message", IntegrationHandlers.SendMessage)
            .WithName(nameof(IntegrationHandlers.SendMessage))
            .WithSummary("Sends a message for a case.")
            .DisableAntiforgery()
            .Accepts<IntegrationHandlers.MessageRequest>("multipart/form-data")
            .LimitUpload(uploadlimit.DefaultMaxFileSizeBytes, string.Join(',', uploadlimit.DefaultAllowedFileExtensions));
        
        group.MapPost("{caseId}/assign", IntegrationHandlers.Assign)
            .WithName(nameof(IntegrationHandlers.Assign))
            .WithSummary("Assign case to a user.");
        
        group.MapPost("{caseId}/approve", IntegrationHandlers.AddApproval)
            .WithName(nameof(IntegrationHandlers.AddApproval))
            .WithSummary("Adds an approval to a case.");
        
        group.MapPost("{caseId}/approve-with-comment", IntegrationHandlers.AddApprovalWithComment)
            .WithName(nameof(IntegrationHandlers.AddApprovalWithComment))
            .WithSummary("Adds an approval with comment to a case translating the provided comment.");
        
        group.MapPost("{caseId}/remove-assignment", IntegrationHandlers.RemoveAssignment)
            .WithName(nameof(IntegrationHandlers.RemoveAssignment))
            .WithSummary("Removes the assigner of a case.");
        
        group.MapPost("{caseId}/block-previous-approver", IntegrationHandlers.BlockPreviousApprover)
            .WithName(nameof(IntegrationHandlers.BlockPreviousApprover))
            .WithSummary("Remove assignment and send a message for the UI.");
        
        group.MapPost("{caseId}/rollback-approval", IntegrationHandlers.RollbackApproval)
            .WithName(nameof(IntegrationHandlers.RollbackApproval))
            .WithSummary("Rollbacks the previous approval of a case.");
        
        // Integrator Endpoints
        group.MapPatch("{caseId}/patch-data", IntegrationHandlers.PatchData)
            .WithName(nameof(IntegrationHandlers.PatchData))
            .WithSummary("Patches the data for a case.")
            .WithDescription(IntegrationHandlers.PatchDataDescription);
        
        group.MapPatch("{caseId}/json-patch-data", IntegrationHandlers.JsonPatchData)
            .WithName(nameof(IntegrationHandlers.JsonPatchData))
            .WithSummary("Patches the data for a case.")
            .WithDescription(IntegrationHandlers.JsonPatchDataDescription);

        group.MapPatch("{caseId}/metadata", IntegrationHandlers.PatchMetadata)
            .WithName(nameof(IntegrationHandlers.PatchMetadata))
            .WithSummary("Patches the metadata of a case.")
            .WithDescription(IntegrationHandlers.PatchMetadataDescription);

        group.MapPost("{caseId}/attach-file", IntegrationHandlers.AttachFile)
            .WithName(nameof(IntegrationHandlers.AttachFile))
            .WithSummary("Attaches a file to a case.")
            .DisableAntiforgery()
            .Accepts<IntegrationHandlers.AttachFileRequest>("multipart/form-data")
            .LimitUpload(uploadlimit.DefaultMaxFileSizeBytes, string.Join(',', uploadlimit.DefaultAllowedFileExtensions));
        
        group.MapGet("{caseId}/attachments", IntegrationHandlers.GetAttachments)
            .WithName(nameof(IntegrationHandlers.GetAttachments))
            .WithSummary("Get a list of Attachments for a CaseId");

        group.MapPost("{caseId}/publish-private-data", IntegrationHandlers.PublishPrivateData)
            .WithName(nameof(IntegrationHandlers.PublishPrivateData))
            .WithSummary("Publish private data to public data of a case.");

        group.MapGet("{caseId}/attachments/{attachmentId:guid}", IntegrationHandlers.GetAttachment)
            .WithName(nameof(IntegrationHandlers.GetAttachment))
            .WithSummary("Get a Case Attachment");
        
        return group;
    }
}