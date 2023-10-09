namespace Indice.Features.Media.AspNetCore.Models.Requests;

/// <summary>The request model used to update file's metadata.</summary>
public class UpdateFileMetadataRequest
{
    /// <summary>The file name.</summary>
    public required string Name { get; set; }

    /// <summary>The file description.</summary>
    public string? Description { get; set; }

    /// <summary>The parent folder Id.</summary>
    public Guid? FolderId { get; set; }
}
