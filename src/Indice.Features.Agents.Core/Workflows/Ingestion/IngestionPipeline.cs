using System.Text;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services;
using Indice.Types;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Ingestion;

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

/// <inheritdoc/>
public class IngestionPipeline : IIngestionPipeline
{
    private readonly IDocumentsService _store;
    private readonly AgentsOptions _options;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;

    /// <summary>Creates a new <see cref="IngestionPipeline"/>.</summary>
    public IngestionPipeline(IDocumentsService store,
        IOptions<AgentsOptions> options, IEmbeddingGenerator<string, Embedding<float>> generator) {
        _store = store;
        _options = options.Value;
        _generator = generator;
    }

    /// <inheritdoc/>
    public async Task<IngestionReport> IngestAsync(IngestRequest request, CancellationToken cancellationToken) {
        using var reader = new StreamReader(request.OpenMarkdownSourceStream(), Encoding.UTF8, leaveOpen: false);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var title = Path.GetFileNameWithoutExtension(request.FileName);
        var (firstCategory, chunks) = request.DocumentType switch {
            DocumentType.MarkdownFaq => FaqChunker.ParseFaq(body),
            DocumentType.Markdown => (null, MarkdownChunker.Chunk(body, title, _options.Ingestion)),
            _ => throw new BusinessException($"Unsupported document type '{request.DocumentType}'.", "UNSUPPORTED_DOCUMENT_TYPE"),
        };

        var effectiveCategory = firstCategory ?? (string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim());
        var effectiveLanguage = string.IsNullOrWhiteSpace(request.Language) ? null : request.Language.Trim();

        if (effectiveCategory is not null && !_options.Taxonomy.Categories.Contains(effectiveCategory, StringComparer.OrdinalIgnoreCase)) {
            throw new BusinessException($"Unknown category '{effectiveCategory}'.", "TAXONOMY_INVALID", [
                $"Allowed categories: {string.Join(", ", _options.Taxonomy.Categories)}.",
            ]);
        }
        if (effectiveLanguage is not null && !_options.Taxonomy.Languages.Contains(effectiveLanguage, StringComparer.OrdinalIgnoreCase)) {
            throw new BusinessException($"Unknown language '{effectiveLanguage}'.", "TAXONOMY_INVALID", [
                $"Allowed languages: {string.Join(", ", _options.Taxonomy.Languages)}.",
            ]);
        }

        byte[]? data = null;
        var toByteArray = (Stream stream) => {
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        };

        if (request.OpenActualSourceStream is not null) {
            using var sourceStream = request.OpenActualSourceStream();
            data = toByteArray(sourceStream);
        } else if (request.Source.StartsWith("local://", StringComparison.OrdinalIgnoreCase)) {
            using var markdownStream = request.OpenMarkdownSourceStream();
            data = toByteArray(markdownStream);
        }

        var document = new IngestedDocument {
            Title = title,
            Source = request.Source,
            Category = effectiveCategory,
            Language = effectiveLanguage,
            ContentHash = FaqChunker.Sha256Hex(string.Concat(title, "\0", effectiveCategory ?? "", "\0", effectiveLanguage ?? "", "\0", body)),
            FileName = request.FileName,
            ContentType = request.ContentType,
            ContentLength = request.ContentLength,
            FileData = data,
            IsPrivate = request.IsPrivate,
        };

        var existing = await _store.FindBySourceAsync(document.Source, includeData: false, cancellationToken);
        if (existing is not null && string.Equals(existing.ContentHash, document.ContentHash, StringComparison.Ordinal)) {
            return new IngestionReport {
                DocumentId = existing.Id,
                Source = document.Source,
                ChunksCreated = 0,
                Skipped = true,
                SkippedReason = "unchanged",
                Replaced = false,
            };
        }

        if (chunks.Count == 0) {
            throw new BusinessException("No content to ingest.", "EMPTY_DOCUMENT");
        }

        var embeddings = await EmbedAsync(chunks.Select(c => c.Content).ToList(), cancellationToken);
        var embedded = chunks.Zip(embeddings, (c, v) => new EmbeddedChunk { Chunk = c, Embedding = v }).ToList();

        var newId = await _store.ReplaceAsync(existing?.Id, document, embedded, cancellationToken);

        return new IngestionReport {
            DocumentId = newId,
            Source = document.Source,
            ChunksCreated = embedded.Count,
            Skipped = false,
            SkippedReason = null,
            Replaced = existing is not null,
        };
    }

    /// <summary>
    /// Embeds <paramref name="texts"/> in batches of <c>IngestionOptions.EmbedBatchSize</c>. Transient 429/5xx
    /// failures are retried by the Azure OpenAI client pipeline; anything that still throws propagates to the
    /// global exception handler.
    /// </summary>
    private async Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken) {
        var result = new ReadOnlyMemory<float>[texts.Count];
        for (var offset = 0; offset < texts.Count; offset += _options.Ingestion.EmbedBatchSize) {
            var batch = texts.Skip(offset).Take(_options.Ingestion.EmbedBatchSize).ToList();
            var embeddings = await _generator.GenerateAsync(batch, cancellationToken: cancellationToken);
            for (var i = 0; i < embeddings.Count; i++) {
                result[offset + i] = embeddings[i].Vector;
            }
        }
        return result;
    }

}
