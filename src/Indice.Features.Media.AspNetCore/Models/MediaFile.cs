namespace Indice.Features.Media.AspNetCore.Models;




/// <summary>Models a file.</summary>
/// <param name="Id">The unique id of the file.</param>
/// <param name="Name">The name of the file.</param>
/// <param name="Path">The path to the file.</param>
/// <param name="Description">The description of the file.</param>
/// <param name="FileExtension">The file extension.</param>
/// <param name="ContentType">The file mime type.</param>
/// <param name="ContentLength">The file size.</param>
/// <param name="Data">The file as a byte array.</param>
/// <param name="FolderId">The Id of the folder containing the file.</param>
/// <param name="PermaLink">The file URI.</param>
/// <param name="CreatedBy">Specifies the principal that created the entity.</param>
/// <param name="CreatedAt">Specifies when an entity was created.</param>
/// <param name="UpdatedBy">Specifies the principal that update the entity.</param>
/// <param name="UpdatedAt">Specifies when an entity was updated.</param>
public record MediaFile(Guid Id, string Name, string Path, string? Description, string FileExtension, string ContentType, int ContentLength, byte[]? Data, Guid? FolderId, string PermaLink, string CreatedBy, DateTimeOffset CreatedAt, string? UpdatedBy, DateTimeOffset? UpdatedAt)
{
    
}
