using System.Net.Mail;
using Indice.Features.Media.AspNetCore.Data.Models;
using Indice.Features.Media.AspNetCore.Models;
using Indice.Features.Media.AspNetCore.Models.Commands;
using Indice.Features.Media.AspNetCore.Models.Requests;
using Indice.Types;

namespace Indice.Features.Media.AspNetCore;
internal static class Mapper
{
    #region Folder
    internal static CreateFolderCommand ToCreateFolderCommand(this CreateFolderRequest request) => new() {
        Name = request.Name,
        Description = request.Description,
        ParentId = request.ParentId
    };
    internal static UpdateFolderCommand ToUpdateFolderCommand(this UpdateFolderRequest request, Guid id) => new() {
        Id = id,
        Name = request.Name,
        Description = request.Description,
        ParentId = request.ParentId
    };
    internal static DbMediaFolder ToDbFolder(this CreateFolderCommand command) => new() {
        Name = command.Name,
        Description = command.Description,
        ParentId = command.ParentId
    };
    internal static void Update(this DbMediaFolder dbFolder, UpdateFolderCommand command) {
        dbFolder.Name = command.Name;
        dbFolder.Description = command.Description;
        dbFolder.ParentId = command.ParentId;
    }
    internal static MediaFolder ToFolder(this DbMediaFolder dbFolder) => new() {
        Id = dbFolder.Id,
        CreatedAt = dbFolder.CreatedAt,
        UpdatedAt = dbFolder.UpdatedAt,
        CreatedBy = dbFolder.CreatedBy,
        UpdatedBy = dbFolder.UpdatedBy,
        Description = dbFolder.Description,
        Name = dbFolder.Name,
        ParentId = dbFolder.ParentId,
        SubFoldersCount = dbFolder.SubFolders?.Count ?? 0,
        FilesCount = dbFolder.Files?.Count ?? 0
    };
    #endregion
    #region File
    internal static UploadFileCommand ToUploadFileCommand(this UploadFileRequest request) {
        if (request.File is null) {
            throw new ArgumentNullException(nameof(File));
        }
        return new UploadFileCommand(request.File.OpenReadStream) {
            Name = Path.GetFileName(request.File.FileName),
            FileExtension = Path.GetExtension(request.File.FileName),
            ContentLength = (int)request.File.Length,
            ContentType = request.File.ContentType,
            FolderId = request.FolderId
        };
        //if (File.Length > 0) {
        //    using var inputStream = File.OpenReadStream();
        //    using var memoryStream = new MemoryStream();
        //    inputStream.CopyTo(memoryStream);
        //    fileParameter.Data = memoryStream.ToArray();
        //}
    }
    internal static UpdateFileMetadataCommand ToUpdateFileMetadataCommand(this UpdateFileMetadataRequest request, Guid id) => new() {
        Id = id,
        Name = request.Name,
        Description = request.Description,
        FolderId = request.FolderId
    };
    internal static DbMediaFile ToDbFile(this UploadFileCommand command) => new() {
        ContentLength = command.ContentLength,
        ContentType = command.ContentType,
        Data = command.Data,
        FileExtension = command.FileExtension,
        Guid = command.Guid,
        Id = command.Id,
        Name = command.Name,
        Uri = command.Uri,
        FolderId = command.FolderId
    };
    internal static void Update(this DbMediaFile dbFile, UpdateFileMetadataCommand command) {
        dbFile.Name = command.Name;
        dbFile.Description = command.Description;
        dbFile.FolderId = command.FolderId;
    }
    internal static MediaFile ToFileDetails(this DbMediaFile dbFile, string permaLinkBaseUrl) => new() {
        ContentLength = dbFile.ContentLength,
        ContentType = dbFile.ContentType,
        FileExtension = dbFile.FileExtension,
        //Guid = dbFile.Guid,
        Id = dbFile.Id,
        Name = dbFile.Name,
        Description = dbFile.Description,
        FolderId = dbFile.FolderId,
        Data = dbFile.Data,
        CreatedAt = dbFile.CreatedAt,
        UpdatedAt = dbFile.UpdatedAt,
        CreatedBy = dbFile.CreatedBy,
        UpdatedBy = dbFile.UpdatedBy,
        PermaLink = $"{permaLinkBaseUrl}/{(Base64Id)dbFile.Guid}.{dbFile.FileExtension.TrimStart('.')}"
    };
    #endregion
}
