namespace Indice.Features.Media.AspNetCore.Models.Commands;
/// <summary> The command used to create a new folder. </summary>
public class CreateFolderCommand
{
    /// <summary> The folder's name. </summary>
    public required string Name { get; set; }
    /// <summary> A short description about the folder's content. </summary>
    public string? Description { get; set; }
    /// <summary> The parent folder id. </summary>
    public Guid? ParentId { get; set; }
}
