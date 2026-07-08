using Indice.Features.Agents.Core.Models;

namespace Indice.Features.Agents.Core.Workflows;

/// <summary>End-to-end ingestion orchestrator: Read → optional skip → Chunk → Embed → atomic Replace.</summary>
public interface IIngestionPipeline
{
    /// <summary>
    /// Ingests a document optionally overriding the category and language. 
    /// If a document with the same source already exists, it will be replaced if the content hash differs; 
    /// otherwise, it will be skipped as a duplicate. Returns a report describing whether the file was ingested, replaced an existing one, or skipped as a duplicate
    /// </summary>
    Task<IngestionReport> IngestAsync(IngestRequest request, CancellationToken cancellationToken);
}
