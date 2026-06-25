
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

    /// <summary>The file to be ingested.</summary>
    public IFormFile File { get; set; } = default!;
}


