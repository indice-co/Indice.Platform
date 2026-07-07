using Indice.Features.Agents.Core.Workflows.Abstractions;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps;

/// <summary>
/// Reorders retrieval candidates by relevance and trims to <c>RetrievalOptions.TopN</c>. Delegates to
/// the registered <see cref="ILlmReranker"/>; bypasses the LLM when reranking is disabled or already
/// at/below the target size.
/// </summary>
public sealed class Reranker : Executor<PipelineStepContext<RetrievalOutput>, PipelineStepContext<RerankOutput>>
{
    private readonly ILlmReranker _reranker;
    private readonly AgentsOptions _options;

    /// <summary>Creates a new <see cref="Reranker"/>.</summary>
    public Reranker(ILlmReranker reranker, IOptions<AgentsOptions> options) : base("Reranker") {
        _reranker = reranker;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public override async ValueTask<PipelineStepContext<RerankOutput>> HandleAsync(PipelineStepContext<RetrievalOutput> envelope, IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        var topResults = _options.Retrieval.NumberOfResults;
        var candidates = envelope.Payload.Candidates;

        IReadOnlyList<RetrievedChunk> reranked = !_options.Pipeline.EnableRerank || candidates.Count <= topResults
            ? candidates.OrderByDescending(c => c.Score).Take(topResults).ToList()
            : await _reranker.RerankAsync(envelope.State.Question, candidates, topResults, cancellationToken);

        return envelope.Next(new RerankOutput {
            Intent = envelope.Payload.Intent,
            RerankedCandidates = reranked,
        });
    }
}
