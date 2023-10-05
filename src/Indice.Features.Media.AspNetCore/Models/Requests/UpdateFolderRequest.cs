namespace Indice.Features.Media.AspNetCore.Models.Requests;

/// <summary>The request model used to update an existing folder.</summary>
public class UpdateFolderRequest
{
    /// <summary>The folder's name.</summary>
    public required string Name { get; set; }
    /// <summary>A short description about the folder.</summary>
    public string? Description { get; set; }
    /// <summary>The folder's parent.</summary>
    public Guid? ParentId { get; set; }
}
