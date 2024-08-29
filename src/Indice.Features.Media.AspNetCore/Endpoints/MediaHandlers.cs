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
    internal static async Task<Results<FileContentHttpResult, NotFound>> DownloadFile(string path, IFileServiceFactory fileServiceFactory) {
        var format = Path.GetExtension(path);
        if (format.StartsWith('.')) {
            format = format.TrimStart('.');
        }
        path = Path.Combine("media/", path.TrimStart('/'));
        var fileService = fileServiceFactory.Create(KeyedServiceNames.FileServiceKey);
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

    internal static async Task<Results<FileContentHttpResult, NotFound>> GetFile(Base64Id fileGuid, string format, MediaManager mediaManager) {
        if (format.StartsWith('.')) {
            format = format.TrimStart('.');
        }
        var file = await mediaManager.GetFileDetails(fileGuid.Id, includeData: true);
        if (file == null) {
            return TypedResults.NotFound();
        }
        var contentType = file.ContentType;
        if (contentType == MediaTypeNames.Application.Octet && !string.IsNullOrEmpty(format)) {
            contentType = FileExtensions.GetMimeType($".{format}");
        }
        return TypedResults.File(file.Data ?? [], contentType, null, false, file.UpdatedAt);
    }

    internal static async Task<Results<Ok<MediaFile>, NotFound>> GetFileDetails(Guid fileId, bool? includeData, MediaManager mediaManager) {
        var file = await mediaManager.GetFileDetails(fileId, includeData);
        if (file is null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(file);
    }

    internal static async Task<Results<Ok<UploadFileResponse>, CreatedAtRoute<UploadFileResponse>>> UploadFile(UploadFileRequest request, MediaManager mediaManager) {
        var fileIds = await mediaManager.UploadFiles(request.ToUploadFileCommand());
        if (fileIds.Count > 1) {
            TypedResults.Ok(new UploadFileResponse(fileIds.ToArray()));
        }
        return TypedResults.CreatedAtRoute(new UploadFileResponse(fileIds.ToArray()), nameof(GetFileDetails), new { fileId = fileIds[0] });
    }

    internal static async Task<NoContent> UpdateFileMetadata(Guid fileId, UpdateFileMetadataRequest request, MediaManager mediaManager) {
        await mediaManager.UpdateFileMetadata(request.ToUpdateFileMetadataCommand(fileId));
        return TypedResults.NoContent();
    }

    internal static async Task<NoContent> DeleteFile(Guid fileId, MediaManager mediaManager) {
        await mediaManager.DeleteFile(fileId);
        return TypedResults.NoContent();
    }
}
