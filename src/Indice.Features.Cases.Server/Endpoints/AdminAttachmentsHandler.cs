using Indice.Features.Cases.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Authorization;
using System.Net.Mime;
using Indice.Features.Cases.Models;
using Indice.Types;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Indice.Features.Cases.Models.Responses;

/// <summary>Manage attachments for a case.</summary>

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminAttachmentsHandler
{
    public static async Task<Results<FileContentHttpResult, NotFound>> DownloadAttachment(Guid attachmentId, IAdminCaseService adminCaseService, ClaimsPrincipal User) {
        var attachment = await adminCaseService.GetDbAttachmentById(User, attachmentId);
        if (attachment is null) {
            return TypedResults.NotFound();
        }
        var fileName = $"{attachmentId}-{DateTimeOffset.UtcNow.Date:dd-MM-yyyy}.{attachment.FileExtension}";
        //TODO: Make sure the result returns 200
        return TypedResults.File(attachment.Data, attachment.ContentType, fileName);
    }
}
