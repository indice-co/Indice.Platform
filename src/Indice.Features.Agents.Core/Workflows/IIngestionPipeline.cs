using Indice.Features.Agents.Core.Models;

namespace Indice.Features.Agents.Core.Workflows;

/// <summary>End-to-end ingestion orchestrator: Read → optional skip → Chunk → Embed → atomic Replace.</summary>
public interface IIngestionPipeline
{
    /// <summary>
    /// Ingests a single markdown stream. Form-data <paramref name="category"/> / <paramref name="language"/>
    /// override any values read from YAML front-matter. Returns a report describing whether the file was
    /// ingested, replaced an existing one, or skipped as a duplicate.
    /// </summary>
    Task<IngestionReport> IngestAsync(Stream content, string fileName, string? category, string? language, CancellationToken cancellationToken);
}
