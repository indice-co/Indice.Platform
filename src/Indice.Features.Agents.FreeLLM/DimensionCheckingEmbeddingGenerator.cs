using Indice.Features.Agents.Core;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.FreeLLM;

/// <summary>
/// Fails early, with a clear message, when the embedding model returns vectors of a width the knowledge store cannot hold
/// (<see cref="AgentsOptions.EmbeddingDimensionsDefault"/>). Without it the mismatch surfaces as an opaque SQL error on insert, or as
/// silently wrong search results.
/// </summary>
internal sealed class DimensionCheckingEmbeddingGenerator : DelegatingEmbeddingGenerator<string, Embedding<float>>
{
    private readonly string _model;

    public DimensionCheckingEmbeddingGenerator(IEmbeddingGenerator<string, Embedding<float>> innerGenerator, string model) : base(innerGenerator) {
        _model = model;
    }

    public override async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default) {
        var embeddings = await base.GenerateAsync(values, options, cancellationToken).ConfigureAwait(false);
        foreach (var embedding in embeddings) {
            if (embedding.Vector.Length != AgentsOptions.EmbeddingDimensionsDefault) {
                throw new InvalidOperationException(
                    $"The embedding model '{_model}' returned {embedding.Vector.Length}-dimensional vectors, but the knowledge store holds {AgentsOptions.EmbeddingDimensionsDefault}-dimensional vectors. " +
                    $"Use a model that produces {AgentsOptions.EmbeddingDimensionsDefault} dimensions, or one that can shorten its output and set {FreeLLMOptions.SectionName}:EmbeddingDimensions to {AgentsOptions.EmbeddingDimensionsDefault}.");
            }
        }
        return embeddings;
    }
}
