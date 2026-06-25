using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Indice.Features.Agents.Core.Workflows.Steps;

/// <summary>
/// Produces up to <c>RetrievalOptions.QueryExpansion</c> alternative phrasings of the user's question to
/// broaden retrieval recall. Disabled by <c>PipelineOptions.EnableQueryRewrite = false</c>; on any LLM
/// failure, falls back to the original question.
/// </summary>
public sealed class QueryRewriter : Executor<PipelineStepContext<IntentOutput>, PipelineStepContext<QueryRewriteOutput>>
{
    private readonly AIAgent _agent;
    private readonly AgentsOptions _options;
    private readonly string _model;

    /// <summary>Creates a new <see cref="QueryRewriter"/>.</summary>
    public QueryRewriter(AzureOpenAIClient openAIClient, IOptions<AgentsOptions> options, IPromptTemplateRenderer prompts) : base("QueryRewriter") {
        _options = options.Value;
        _model = _options.AzureOpenAI.Deployments.Fast!;
        _agent = openAIClient
            .GetChatClient(_model)
            .AsAIAgent(options: new ChatClientAgentOptions() {
                ChatOptions = new ChatOptions {
                    Temperature = _options.Models.Fast.Temperature,
                    MaxOutputTokens = _options.Models.Fast.MaxOutputTokens,
                    Instructions = prompts.Render("QueryRewriter"),
                },
                Name = "DexQueryRewriter",
            });
    }

    /// <inheritdoc/>
    public override async ValueTask<PipelineStepContext<QueryRewriteOutput>> HandleAsync(
        PipelineStepContext<IntentOutput> envelope,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        var question = envelope.State.Question;
        var expansion = _options.Retrieval.QueryExpansion;
        var enabled = _options.Pipeline.EnableQueryRewrite && expansion > 1;

        var queries = new List<string> { question };
        if (enabled) {
            try {
                var history = ChatHistoryFormatter.Format(envelope.State.History);
                var historyBlock = history.Length == 0 ? string.Empty : $"HISTORY:\n{history}\n";
                var prompt = $"{historyBlock}Question: {question}\n\nProduce {expansion - 1} alternative rewrite(s).";
                var response = await _agent.RunAsync<RewriteResult>(prompt, cancellationToken: cancellationToken);
                foreach (var q in response.Result.Queries) {
                    if (!string.IsNullOrWhiteSpace(q) && !queries.Contains(q, StringComparer.OrdinalIgnoreCase)) {
                        queries.Add(q);
                    }
                    if (queries.Count >= expansion) break;
                }
                // Fast-model usage is intentionally not tracked — only reasoning-model tokens are persisted.
            } 
            catch (OperationCanceledException) {
                throw;
            }
            catch {
                // Fall back to just the original question.
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
