namespace Indice.Features.Media.AspNetCore.Models.Requests;

/// <summary>The request model used to create a new folder.</summary>
public class CreateFolderRequest
{
    /// <summary>The folder's name.</summary>
    public required string Name { get; set; }
    /// <summary>A short description about the folder.</summary>
    public string? Description { get; set; }
    /// <summary>The folder's parent.</summary>
    public Guid? ParentId { get; set; }
}
