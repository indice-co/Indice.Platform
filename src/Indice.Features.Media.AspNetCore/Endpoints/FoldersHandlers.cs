using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Indice.Features.Media.AspNetCore.Models;
using Indice.Features.Media.AspNetCore.Models.Requests;

namespace Indice.Features.Media.AspNetCore.Endpoints;
internal static class FoldersHandlers
{
    internal static async Task<Ok<FolderTreeStructure>> GetFolderStructure(MediaManager mediaManager) {
        var structure = await mediaManager.GetFolderTreeStructure();
        return TypedResults.Ok(structure);
    }

    internal static async Task<Results<Ok<FolderContent>, NotFound>> GetFolderContent(Guid? folderId, int? page, int? size, MediaManager mediaManager) {
        if (folderId.HasValue) {
            var folder = await mediaManager.GetFolderById(folderId.Value);
            if (folder == null) {
                return TypedResults.NotFound();
            }
        }
        var folderContent = await mediaManager.GetFolderContent(folderId, page, size);
        
        return TypedResults.Ok(folderContent);
    }

    internal static async Task<Ok<List<MediaFolder>>> ListFolders(MediaManager mediaManager) {
        var folders = await mediaManager.ListFolders();
        return TypedResults.Ok(folders);
    }

    internal static async Task<Results<Ok<MediaFolder>, NotFound>> GetFolderById(Guid folderId, MediaManager mediaManager) {
        var folder = await mediaManager.GetFolderById(folderId);
        if (folder == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(folder);
    }

    internal static async Task<CreatedAtRoute<Guid>> CreateFolder(CreateFolderRequest request, MediaManager mediaManager) {
        var folderId = await mediaManager.CreateFolder(request.ToCreateFolderCommand());
        return TypedResults.CreatedAtRoute(folderId, nameof(CreateFolder), new { folderId });
    }

    internal static async Task<Ok> UpdateFolder(Guid folderId, UpdateFolderRequest request, MediaManager mediaManager) {
        await mediaManager.UpdateFolder(request.ToUpdateFolderCommand(folderId));
        return TypedResults.Ok();
    }

    internal static async Task<Ok> DeleteFolder(Guid folderId, MediaManager mediaManager) {
        await mediaManager.DeleteFolder(folderId);
        return TypedResults.Ok();
    }
}
