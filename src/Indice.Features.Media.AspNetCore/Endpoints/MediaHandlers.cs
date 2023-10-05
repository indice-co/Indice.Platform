using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Indice.Features.Media.AspNetCore.Models;
using Indice.Features.Media.AspNetCore.Models.Requests;
using Indice.Services;
using System.Net.Mime;
using Indice.Extensions;
using Microsoft.Net.Http.Headers;
using Indice.Types;

namespace Indice.Features.Media.AspNetCore.Endpoints;
internal static class MediaHandlers
{
    internal static async Task<Results<FileContentHttpResult, NotFound>> GetFile(Base64Id fileGuid, string format, Func<string, IFileService> getFileService) {
        if (format.StartsWith('.')) {
            format = format.TrimStart('.');
        }
        var fileService = getFileService(KeyedServiceNames.FileServiceKey);
        var path = $"media/{fileGuid.Id.ToString("N")[..2]}/{fileGuid.Id:N}.{format}";
        var properties = await fileService.GetPropertiesAsync(path);
        if (properties is null) {
            return TypedResults.NotFound();
        }
        var data = await fileService.GetAsync(path);
        var contentType = properties.ContentType;
        if (contentType == MediaTypeNames.Application.Octet && !string.IsNullOrEmpty(format)) {
            contentType = FileExtensions.GetMimeType($".{format}");
        }
        return TypedResults.File(data, contentType, null, false,  properties.LastModified, new EntityTagHeaderValue(properties.ETag, true));
    }

    internal static async Task<Results<Ok<MediaFile>, NotFound>> GetFileDetails(Guid fileId, bool? includeData, MediaManager mediaManager) {
        var file = await mediaManager.GetFileDetails(fileId, includeData);
        if (file is null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(file);
    }

    internal static async Task<CreatedAtRoute<Guid>> UploadFile(UploadFileRequest request, MediaManager mediaManager) {
        var fileId = await mediaManager.UploadFile(request.ToUploadFileCommand());
        return TypedResults.CreatedAtRoute(fileId, nameof(GetFileDetails), new { fileId });
    }

    internal static async Task<Ok> UpdateFileMetadata(Guid fileId, UpdateFileMetadataRequest request, MediaManager mediaManager) {
        await mediaManager.UpdateFileMetadata(request.ToUpdateFileMetadataCommand(fileId));
        return TypedResults.Ok();
    }

    internal static async Task<Ok> DeleteFile(Guid fileId, MediaManager mediaManager) {
        await mediaManager.DeleteFile(fileId);
        return TypedResults.Ok();
    }
}
