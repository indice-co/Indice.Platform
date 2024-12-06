using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;
using Indice.Features.Cases.Core.Services.Abstractions;

namespace Indice.Features.Cases.Server.Endpoints;

/// <summary>Manage attachments for a case.</summary>
internal static class AdminAttachmentsHandler
{
    public static async Task<Results<FileContentHttpResult, NotFound>> DownloadAttachment(
        Guid attachmentId, 
        IAdminCaseService adminCaseService, 
        ClaimsPrincipal currentUser) {
        var attachment = await adminCaseService.GetAttachmentById(currentUser, attachmentId);
        if (attachment is null) {
            return TypedResults.NotFound();
        }
        var fileName = $"{attachmentId}-{DateTimeOffset.UtcNow.Date:dd-MM-yyyy}.{attachment.FileExtension}";
        //TODO: Make sure the result returns 200
        return TypedResults.File(attachment.Data!, attachment.ContentType, fileName);
    }
}
