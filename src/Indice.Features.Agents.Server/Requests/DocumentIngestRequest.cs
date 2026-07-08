
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Agents.Core.Models.Requests;

/// <summary>Represents a request to ingest a document into the system.</summary>
public class DocumentIngestRequest
{
    /// <summary>The type of the document.</summary>
    public DocumentType DocumentType { get; set; }

    /// <summary>The category of the document.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>The language of the document.</summary>
    public string Language { get; set; } = string.Empty;
    /// <summary>The content to be ingested.</summary>
    public IFormFile ContentMarkdown { get; set; } = default!;
    /// <summary>The file to be archived.</summary>
    public IFormFile? SourceFile { get; set; }
    /// <summary>The source URL of the document. This can be used for provenance and traceability. It can be an external URL and it is mutually exclusive with the SourceFile property.</summary>
    public string? SourceUrl { get; set; }
}


