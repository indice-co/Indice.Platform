using System.Text;
using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows.Events;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.State;
using Indice.Features.Agents.Core.Workflows.Usage;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Indice.Features.Agents.Core.Workflows.Steps;

/// <summary>
/// Composes the final grounded answer from the reranked candidates. Instructs the model to ground only
/// in the provided context and cite chunk IDs in <c>[#chunkId]</c> form. Projects the candidates into
/// <see cref="Citation"/> records on the output payload.
/// </summary>
public sealed class AnswerComposer : Executor<PipelineStepContext<RerankOutput>, PipelineStepContext<RagPipelineOutput>>
{
    private readonly AIAgent _agent;
    private readonly AgentsOptions _options;
    private readonly string _model;

    /// <summary>Creates a new <see cref="AnswerComposer"/>.</summary>
    public AnswerComposer(AzureOpenAIClient openAIClient, IOptions<AgentsOptions> options, IPromptTemplateRenderer prompts,
        UserClaimsAIContextProvider userClaimsProvider, TokenUsageAccumulator usage) : base("AnswerComposer") {
        _options = options.Value;
        _model = _options.AzureOpenAI.Deployments.Reasoning!;

        var instructions = prompts.Render("AnswerComposer", new {
            strictGrounding = _options.Pipeline.StrictGrounding,
        });

        _agent = openAIClient
            .GetChatClient(_model)
            .AsAIAgent(
                options: new ChatClientAgentOptions() {
                    ChatOptions = new ChatOptions {
                        Temperature = _options.Models.Reasoning.Temperature,
                        MaxOutputTokens = _options.Models.Reasoning.MaxOutputTokens,
                        Instructions = instructions,
                    },
                    AIContextProviders = [userClaimsProvider],
                    Name = "DexAnswerComposer"
                },
                clientFactory: inner => new UsageTrackingChatClient(inner, usage, _model));
    }

    /// <inheritdoc/>
    public override async ValueTask<PipelineStepContext<RagPipelineOutput>> HandleAsync(
        PipelineStepContext<RerankOutput> envelope,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        var candidates = envelope.Payload.RerankedCandidates;
        var prompt = BuildPrompt(envelope.State.Question, envelope.State.History, candidates);

        // Stream the answer: emit each text delta as a workflow event (surfaced as an SSE `delta` by the
        // streaming runner; ignored by the non-streaming runner) while accumulating the full text.
        var answer = new StringBuilder();
        await foreach (var update in _agent.RunStreamingAsync(prompt, cancellationToken: cancellationToken)) {
            if (!string.IsNullOrEmpty(update.Text)) {
                answer.Append(update.Text);
                await context.AddEventAsync(new AnswerDeltaEvent(update.Text), cancellationToken);
            }
        }

        var citations = candidates
            .Select(c => new Citation {
                ChunkId = c.ChunkId,
                DocumentId = c.DocumentId,
                Title = c.Title,
                HeadingPath = c.HeadingPath,
                Score = c.Score,
            })
            .ToList();

        // Token usage is captured by UsageTrackingChatClient (reasoning client) and read by DexRunner after the run.
        return envelope.Next(new RagPipelineOutput {
            Answer = answer.ToString(),
            Citations = citations,
        });
    }

    private static string BuildPrompt(string question, IReadOnlyList<Models.ChatMessage> history, IReadOnlyList<RetrievedChunk> candidates) {
        var sb = new StringBuilder();
        var historyText = ChatHistoryFormatter.Format(history);
        if (historyText.Length > 0) {
            sb.AppendLine("HISTORY:").Append(historyText).AppendLine();
        }
        sb.AppendLine("CONTEXT:");
        if (candidates.Count == 0) {
            sb.AppendLine("(no candidates retrieved)");
        }
        foreach (var c in candidates) {
            sb.Append("[#").Append(c.ChunkId).Append("] ");
            if (!string.IsNullOrWhiteSpace(c.Title)) {
                sb.Append(c.Title).Append(" — ");
            }
            sb.AppendLine(c.Content);
            sb.AppendLine();
        }
        sb.AppendLine();
        sb.Append("QUESTION: ").AppendLine(question);
        return sb.ToString();
    }
}
