using Indice.Features.Agents.Core;
using Indice.Types;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using static Indice.Features.Agents.Core.AgentsOptions;

namespace Indice.Features.Agents.Core.Workflows;

/// <inheritdoc/>
public class AzureOpenAIEmbedder : IEmbedder
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;
    private readonly IngestionOptions _options;

    /// <summary>Creates a new <see cref="AzureOpenAIEmbedder"/>.</summary>
    public AzureOpenAIEmbedder(IEmbeddingGenerator<string, Embedding<float>> generator, IOptions<AgentsOptions> options) {
        _generator = generator;
        _options = options.Value.Ingestion;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken) {
        if (texts.Count == 0) {
            return Array.Empty<ReadOnlyMemory<float>>();
        }
        var result = new ReadOnlyMemory<float>[texts.Count];
        for (var offset = 0; offset < texts.Count; offset += _options.EmbedBatchSize) {
            var batch = texts.Skip(offset).Take(_options.EmbedBatchSize).ToList();
            var embeddings = await GenerateWithRetryAsync(batch, cancellationToken);
            for (var i = 0; i < embeddings.Count; i++) {
                result[offset + i] = embeddings[i].Vector;
            }
        }
        return result;
    }

    private async Task<GeneratedEmbeddings<Embedding<float>>> GenerateWithRetryAsync(IReadOnlyList<string> batch, CancellationToken cancellationToken) {
        for (var attempt = 0; attempt <= _options.MaxRetries ; attempt++) {
            try {
                return await _generator.GenerateAsync(batch, options: null, cancellationToken);
            } 
            catch (Exception ex) when (attempt < _options.MaxRetries && IsTransient(ex)) {
                await Task.Delay(_options.RetryDelays[attempt % _options.RetryDelays.Length], cancellationToken);
            } 
            catch (Exception ex) {
                throw new BusinessException("Embedding service unavailable.", "EMBEDDING_FAILED", [ex.Message]);
            }
        }
        return new GeneratedEmbeddings<Embedding<float>>();
    }

    private static bool IsTransient(Exception ex) {
        // Azure OpenAI surfaces 429 / 5xx through HttpRequestException / RequestFailedException; treat both as retryable.
        var message = ex.Message ?? string.Empty;
        return ex is HttpRequestException
            || message.Contains("429", StringComparison.Ordinal)
            || message.Contains("500", StringComparison.Ordinal)
            || message.Contains("502", StringComparison.Ordinal)
            || message.Contains("503", StringComparison.Ordinal)
            || message.Contains("504", StringComparison.Ordinal);
    }
}
