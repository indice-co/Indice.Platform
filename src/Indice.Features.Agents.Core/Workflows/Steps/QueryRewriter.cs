using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps;

/// <summary>
/// Produces up to <c>RetrievalOptions.QueryExpansion</c> alternative phrasings of the user's question to
/// broaden retrieval recall. Disabled by <c>DefaultPipelineOptions.EnableQueryRewrite = false</c>; on any LLM
/// failure, falls back to the original question.
/// </summary>
public sealed class QueryRewriter : Executor<IntentOutput, QueryRewriteOutput>
{
    private readonly AIAgent _agent;
    private readonly AgentsOptions _options;
    private readonly string _model;

    /// <summary>Creates a new <see cref="QueryRewriter"/>.</summary>
    public QueryRewriter(
        [FromKeyedServices(nameof(AgentsOptions.AzureOpenAIDeployments.Fast))] IChatClient chatClient, 
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models, IPromptTemplateRenderer prompts,
        SessionStoreChatHistoryProvider historyProvider) : base("QueryRewriter") {
        _options = options.Value;
        _model = _options.AzureOpenAI.Deployments.Fast!;
        var chatOptions = models.Value.BaseFastModelOptions.Clone();
        chatOptions.Instructions = prompts.Render("QueryRewriter");
        _agent = chatClient
            .AsAIAgent(options: new ChatClientAgentOptions() {
                ChatOptions = chatOptions,
                Name = "DexQueryRewriter",
                ChatHistoryProvider = historyProvider,
            });
    }

    /// <inheritdoc/>
    public override async ValueTask<QueryRewriteOutput> HandleAsync(IntentOutput intentResult,
        IWorkflowContext context, CancellationToken cancellationToken = default) {
        var state = await context.GetConversationStateAsync(cancellationToken);
        var expansion = _options.Retrieval.QueryExpansion;
        var enabled = _options.Pipeline.EnableQueryRewrite && expansion > 1;

        var queries = new List<string> { state.Message.Text };
        if (enabled) {
            var agentSession = await _agent.CreateSessionAsync(cancellationToken);
            SessionStoreChatHistoryProvider.SetSessionId(agentSession, Guid.Parse(state.ConversationId));
            var prompt = $"Question: {state.Message.Text}\n\nProduce {expansion - 1} alternative rewrite(s).";
            var response = await _agent.RunAsync<RewriteResult>(prompt, agentSession, cancellationToken: cancellationToken);
            foreach (var q in response.Result.Queries) {
                if (!string.IsNullOrWhiteSpace(q) && !queries.Contains(q, StringComparer.OrdinalIgnoreCase)) {
                    queries.Add(q);
                }
                if (queries.Count >= expansion) break;
            }
        }
        return new QueryRewriteOutput(
            intentResult.Intent,
            intentResult.Filters,
            queries
        );
    }

    private sealed class RewriteResult
    {
        public List<string> Queries { get; set; } = new();
    }
}

/// <summary>Output payload of <c>QueryRewriter</c>.</summary>
/// <param name="Intent">The classified intent, forwarded from upstream.</param>
/// <param name="Filters">Retrieval filters, forwarded from upstream.</param>
/// <param name="RewrittenQueries">One or more reworded versions of the original question to be embedded and searched. Always contains at least the original.</param>
public record QueryRewriteOutput(Intent Intent, RetrievalFilters Filters, IReadOnlyList<string> RewrittenQueries);