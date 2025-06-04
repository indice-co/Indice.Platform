namespace Indice.Features.Cases.Core.Models;

/// <summary>A success response message.</summary>
public class SuccessMessage
{
    /// <summary>The message's Title.</summary>
    public string Title { get; set; } = null!;

    /// <summary>The message's Body.</summary>
    public string? Body { get; set; }
}