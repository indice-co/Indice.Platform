using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps;

/// <summary>
/// Produces up to <c>RetrievalOptions.QueryExpansion</c> alternative phrasings of the user's question to
/// broaden retrieval recall. Disabled by <c>DefaultPipelineOptions.EnableQueryRewrite = false</c>; on any LLM
/// failure, falls back to the original question.
/// </summary>
public sealed class QueryRewriter : Executor<PipelineStepContext<IntentOutput>, PipelineStepContext<QueryRewriteOutput>>
{
    private readonly AIAgent _agent;
    private readonly AgentsOptions _options;

    /// <summary>Creates a new <see cref="QueryRewriter"/>.</summary>
    public QueryRewriter(IAgentsFactory agents, IOptions<AgentsOptions> options) : base("QueryRewriter") {
        _options = options.Value;
        _agent = agents.Create(new AgentDescriptor {
            Name = "DexQueryRewriter",
            Role = AgentModelRole.Fast,
            PromptTemplate = "QueryRewriter",
        });
    }

    /// <inheritdoc/>
    public override async ValueTask<PipelineStepContext<QueryRewriteOutput>> HandleAsync(PipelineStepContext<IntentOutput> envelope,
        IWorkflowContext context, CancellationToken cancellationToken = default) {
        var question = envelope.State.Question;
        var expansion = _options.Retrieval.QueryExpansion;
        var enabled = _options.Pipeline.EnableQueryRewrite && expansion > 1;

        var queries = new List<string> { question };
        if (enabled) {
            var agentSession = await _agent.CreateSessionAsync(cancellationToken);
            SessionStoreChatHistoryProvider.SetSessionId(agentSession, envelope.State.SessionId);
            var prompt = $"Question: {question}\n\nProduce {expansion - 1} alternative rewrite(s). Use the provided History to inform your rewrites.";
            var response = await _agent.RunAsync<RewriteResult>(prompt, agentSession, cancellationToken: cancellationToken);
            foreach (var q in response.Result.Queries) {
                if (!string.IsNullOrWhiteSpace(q) && !queries.Contains(q, StringComparer.OrdinalIgnoreCase)) {
                    queries.Add(q);
                }
                if (queries.Count >= expansion) break;
            }
        }
        return envelope.Next(new QueryRewriteOutput {
            Intent = envelope.Payload.Intent,
            Filters = envelope.Payload.Filters,
            RewrittenQueries = queries,
        });
    }

    private sealed class RewriteResult
    {
        public List<string> Queries { get; set; } = new();
    }
}
