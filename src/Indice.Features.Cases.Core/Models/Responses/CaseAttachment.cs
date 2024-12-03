namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>Minimal Case Attachment response model.</summary>
public class CaseAttachment
{
    /// <summary>The Id of the attachment.</summary>
    public Guid Id { get; set; }

    /// <summary>The name of the attachment.</summary>
    public string? FileName { get; set; }

    /// <summary>The content type of the attachment.</summary>
    public string? ContentType { get; set; }

    /// <summary>The extension of the attachment.</summary>
    public string? FileExtension { get; set; }

    /// <summary>The binary data of the attachment.</summary>
    public byte[]? Data { get; set; }
}