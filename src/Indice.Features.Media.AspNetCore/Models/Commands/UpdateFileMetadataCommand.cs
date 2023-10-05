namespace Indice.Features.Media.AspNetCore.Models.Commands;
/// <summary> The command used to update the file's metadata. </summary>
public class UpdateFileMetadataCommand
{
    /// <summary> The file's Id. </summary>
    public Guid Id { get; set; }
    /// <summary> The file's name. </summary>
    public required string Name { get; set; }
    /// <summary> A short description about the file's usage. </summary>
    public string? Description { get; set; }
    /// <summary> The parent folder's Id. </summary>
    public Guid? FolderId { get; set; }
}
