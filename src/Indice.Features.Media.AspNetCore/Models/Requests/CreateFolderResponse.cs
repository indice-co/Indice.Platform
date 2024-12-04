namespace Indice.Features.Media.AspNetCore.Models.Requests;
/// <summary>
/// Represents the The folder created response with the folder Id.
/// </summary>
/// <param name="FolderId">The folder Id for the folder created</param>
public record CreateFolderResponse(Guid FolderId);
