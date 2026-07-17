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
public sealed class Reranker : Executor<RetrievalOutput, RerankOutput>
{
    private readonly ILlmReranker _reranker;
    private readonly AgentsOptions _options;

    /// <summary>Creates a new <see cref="Reranker"/>.</summary>
    public Reranker(ILlmReranker reranker, IOptions<AgentsOptions> options) : base("Reranker") {
        _reranker = reranker;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public override async ValueTask<RerankOutput> HandleAsync(RetrievalOutput retrievalOutput, IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        var topResults = _options.Retrieval.NumberOfResults;
        var candidates = retrievalOutput.Candidates;
        var state = await context.GetConversationStateAsync(cancellationToken);
        var intentState = await context.GetIntentStateAsync(cancellationToken);
        IReadOnlyList<RetrievedChunk> reranked = !_options.Pipeline.EnableRerank || candidates.Count <= topResults
            ? candidates.OrderByDescending(c => c.Score).Take(topResults).ToList()
            : await _reranker.RerankAsync(state.Message.Text, candidates, topResults, cancellationToken);
        return new RerankOutput(
            intentState.Intent,
            reranked
        );
    }
}

/// <summary>Output payload of <c>Reranker</c>.</summary>
/// <param name="Intent">The classified intent, forwarded from upstream.</param>
/// <param name="RerankedCandidates">Top-N candidates reordered by reranker score; their <c>Score</c> reflects the rerank outcome.</param>
public record RerankOutput(Intent Intent, IReadOnlyList<RetrievedChunk> RerankedCandidates);
