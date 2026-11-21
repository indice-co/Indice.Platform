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
    private readonly bool _truncate;

    /// <param name="innerGenerator">The generator to check.</param>
    /// <param name="model">The model name, for the error message.</param>
    /// <param name="truncate">Shorten longer vectors to the store's width (and re-normalize them) instead of rejecting them.</param>
    public DimensionCheckingEmbeddingGenerator(IEmbeddingGenerator<string, Embedding<float>> innerGenerator, string model, bool truncate = false) : base(innerGenerator) {
        _model = model;
        _truncate = truncate;
    }

    public override async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default) {
        var embeddings = await base.GenerateAsync(values, options, cancellationToken).ConfigureAwait(false);
        for (var i = 0; i < embeddings.Count; i++) {
            var length = embeddings[i].Vector.Length;
            if (length == AgentsOptions.EmbeddingDimensionsDefault) {
                continue;
            }
            // Only longer vectors can be shortened; a shorter one cannot be padded without changing what it means.
            if (_truncate && length > AgentsOptions.EmbeddingDimensionsDefault) {
                embeddings[i] = Truncate(embeddings[i]);
                continue;
            }
            var hint = length > AgentsOptions.EmbeddingDimensionsDefault
                ? $"If the model supports shortened vectors (Matryoshka embeddings, e.g. gemini-embedding-001 or text-embedding-3), set {FreeLLMOptions.SectionName}:Compatibility:TruncateEmbeddings to true, " +
                  $"or {FreeLLMOptions.SectionName}:EmbeddingDimensions to {AgentsOptions.EmbeddingDimensionsDefault} when the server honors the dimensions parameter. Otherwise use a model that produces {AgentsOptions.EmbeddingDimensionsDefault} dimensions."
                : $"Use a model that produces {AgentsOptions.EmbeddingDimensionsDefault} dimensions.";
            throw new InvalidOperationException(
                $"The embedding model '{_model}' returned {length}-dimensional vectors, but the knowledge store holds {AgentsOptions.EmbeddingDimensionsDefault}-dimensional vectors. {hint}");
        }
        return embeddings;
    }

    /// <summary>Keeps the first <see cref="AgentsOptions.EmbeddingDimensionsDefault"/> values and rescales them to unit length, as shortened Matryoshka vectors require.</summary>
    private static Embedding<float> Truncate(Embedding<float> embedding) {
        var shortened = embedding.Vector.Span[..AgentsOptions.EmbeddingDimensionsDefault].ToArray();
        var norm = MathF.Sqrt(shortened.Sum(value => value * value));
        if (norm > 0) {
            for (var i = 0; i < shortened.Length; i++) {
                shortened[i] /= norm;
            }
        }
        return new Embedding<float>(shortened) {
            CreatedAt = embedding.CreatedAt,
            ModelId = embedding.ModelId,
            AdditionalProperties = embedding.AdditionalProperties
        };
    }
}
