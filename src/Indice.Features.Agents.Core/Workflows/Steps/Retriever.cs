using Indice.Features.Agents.Core.Services;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Indice.Features.Agents.Core.Workflows.State;

namespace Indice.Features.Agents.Core.Workflows.Steps;

/// <summary>
/// Embeds each rewritten query and retrieves the top-K most cosine-similar chunks via <see cref="IDocumentsService"/>.
/// Unions and deduplicates across rewrites by <c>ChunkId</c>, keeping the highest score.
/// </summary>
public sealed class Retriever : Executor<PipelineStepContext<QueryRewriteOutput>, PipelineStepContext<RetrievalOutput>>
{
    private readonly IDocumentsService _documentsService;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embedder;
    private readonly AgentsOptions _options;

    /// <summary>Creates a new <see cref="Retriever"/>.</summary>
    public Retriever(
        IDocumentsService documentsService,
        IEmbeddingGenerator<string, Embedding<float>> embedder,
        IOptions<AgentsOptions> options) : base("Retriever") {
        _documentsService = documentsService;
        _embedder = embedder;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public override async ValueTask<PipelineStepContext<RetrievalOutput>> HandleAsync(
        PipelineStepContext<QueryRewriteOutput> envelope,IWorkflowContext context, CancellationToken cancellationToken = default) {
        var filters = envelope.Payload.Filters;
        var topK = _options.Retrieval.NumberOfCandidates;

        var relevantAnswers = new Dictionary<Guid, RetrievedChunk>();
        foreach (var query in envelope.Payload.RewrittenQueries) {
            // Embedding dimensions are configured once on the generator registration (AddAgentsCore).
            var vector = await _embedder.GenerateVectorAsync(query, cancellationToken: cancellationToken);
            var hits = await _documentsService.SearchAsync(vector, filters, topK, _options.Retrieval.MinScore, cancellationToken);
            //kinda optional , but if the same chunk is returned for multiple rewrites, we want to keep the highest score
            //could also become a weighted function of the score and the rewrite rank, but let's keep it simple for now
            foreach (var hit in hits.Where(hit => !relevantAnswers.TryGetValue(hit.ChunkId, out var prior) || hit.Score > prior.Score)) {
                relevantAnswers[hit.ChunkId] = hit;
            }
        }
        var candidates = relevantAnswers.Values
            .OrderByDescending(c => c.Score)
            .ToList();
        return envelope.Next(new RetrievalOutput {
            Intent = envelope.Payload.Intent,
            Candidates = candidates,
        });
    }
}
