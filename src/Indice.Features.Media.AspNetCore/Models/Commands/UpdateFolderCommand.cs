namespace Indice.Features.Media.AspNetCore.Models.Commands;
/// <summary> The command used to update a folder. </summary>
public class UpdateFolderCommand
{
    /// <summary> The folder's Id. </summary>
    public Guid Id { get; set; }
    /// <summary> The folder's name. </summary>
    public required string Name { get; set; }
    /// <summary> A short description about the folder's content. </summary>
    public string? Description { get; set; }
    /// <summary> The parent folder id. </summary>
    public Guid? ParentId { get; set; }
}
