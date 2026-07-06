using System.Security.Cryptography;
using System.Text;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services;
using Indice.Types;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows;

/// <inheritdoc/>
public class DefaultIngestionPipeline : IIngestionPipeline
{
    private readonly IDocumentsService _store;
    private readonly AgentsOptions _options;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;

    /// <summary>Creates a new <see cref="DefaultIngestionPipeline"/>.</summary>
    public DefaultIngestionPipeline(IDocumentsService store,
        IOptions<AgentsOptions> options, IEmbeddingGenerator<string, Embedding<float>> generator) {
        _store = store;
        _options = options.Value;
        _generator = generator;
    }

    /// <inheritdoc/>
    public async Task<IngestionReport> IngestAsync(Stream content, string fileName, string? category, string? language, CancellationToken cancellationToken) {
        using var reader = new StreamReader(content, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var (firstCategory, chunks) = ParseFaq(body);

        var effectiveCategory = firstCategory ?? (string.IsNullOrWhiteSpace(category) ? null : category.Trim());
        var effectiveLanguage = string.IsNullOrWhiteSpace(language) ? null : language.Trim();

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

        var title = Path.GetFileNameWithoutExtension(fileName);
        var document = new IngestedDocument {
            Title = title,
            Source = fileName,
            Category = effectiveCategory,
            Language = effectiveLanguage,
            ContentHash = Sha256Hex(string.Concat(title, "\0", effectiveCategory ?? "", "\0", effectiveLanguage ?? "", "\0", body)),
        };

        var existing = await _store.FindBySourceAsync(document.Source, cancellationToken);
        if (existing.HasValue && string.Equals(existing.Value.ContentHash, document.ContentHash, StringComparison.Ordinal)) {
            return new IngestionReport {
                DocumentId = existing.Value.Id,
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
            Replaced = existing.HasValue,
        };
    }

    /// <summary>
    /// Embeds <paramref name="texts"/> in batches of <c>IngestionOptions.EmbedBatchSize</c>. Transient 429/5xx
    /// failures are retried by the Azure OpenAI client pipeline; anything that still throws surfaces as a
    /// <see cref="BusinessException"/> so the endpoint responds 422 instead of 500.
    /// </summary>
    private async Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken) {
        var result = new ReadOnlyMemory<float>[texts.Count];
        for (var offset = 0; offset < texts.Count; offset += _options.Ingestion.EmbedBatchSize) {
            var batch = texts.Skip(offset).Take(_options.Ingestion.EmbedBatchSize).ToList();
            GeneratedEmbeddings<Embedding<float>> embeddings;
            try {
                embeddings = await _generator.GenerateAsync(batch, cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) {
                throw new BusinessException("Embedding service unavailable.", "EMBEDDING_FAILED", [ex.Message]);
            }
            for (var i = 0; i < embeddings.Count; i++) {
                result[offset + i] = embeddings[i].Vector;
            }
        }
        return result;
    }

    /// <summary>
    /// Walks <paramref name="body"/> line by line, recognizing ATX <c># </c> as a category boundary and
    /// <c>## </c> as a question boundary. Body until the next <c>##</c>/<c>#</c>/EOF is the answer.
    /// Returns the first <c>#</c> (document-level category) and one chunk per Q&amp;A pair.
    /// </summary>
    private static (string? FirstCategory, IReadOnlyList<DocumentChunk> Chunks) ParseFaq(string body) {
        var chunks = new List<DocumentChunk>();
        string? firstCategory = null;
        string? currentCategory = null;
        string? pendingQuestion = null;
        var pendingAnswer = new StringBuilder();
        var chunkIndex = 0;

        foreach (var line in body.Split(['\n', '\r'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)) {
            
            if (line.StartsWith("## ", StringComparison.Ordinal)) {
                Flush();
                pendingQuestion = line[3..].TrimStart();
                continue;
            }
            if (line.StartsWith("# ", StringComparison.Ordinal)) {
                Flush();
                currentCategory = line[2..].TrimStart();
                firstCategory ??= currentCategory;
                continue;
            }
            // Lines before any `##` are silently discarded.
            if (pendingQuestion is null) {
                continue;
            }
            if (pendingAnswer.Length > 0) {
                pendingAnswer.Append('\n');
            }
            pendingAnswer.Append(line);
        }
        Flush();

        return (firstCategory, chunks);

        void Flush() {
            if (string.IsNullOrWhiteSpace(pendingQuestion)) {
                pendingQuestion = null;
                pendingAnswer.Clear();
                return;
            }
            var answer = pendingAnswer.ToString().Trim();
            if (answer.Length == 0) {
                pendingQuestion = null;
                pendingAnswer.Clear();
                return;
            }
            var embedded = $"Q: {pendingQuestion}\nA: {answer}";
            var headingPath = string.IsNullOrEmpty(currentCategory)
                ? pendingQuestion!
                : $"{currentCategory} > {pendingQuestion}";
            chunks.Add(new DocumentChunk {
                ChunkIndex = chunkIndex++,
                Content = embedded,
                ContentHash = Sha256Hex(embedded),
                HeadingPath = headingPath,
                Title = pendingQuestion,
                Category = currentCategory,
                TokenCount = 0,
            });
            pendingQuestion = null;
            pendingAnswer.Clear();
        }
    }

    private static string Sha256Hex(string input) {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
