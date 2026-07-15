using System.Text;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.Events;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps;

/// <summary>
/// Composes the final grounded answer from the reranked candidates. Instructs the model to ground only
/// in the provided context and cite chunk IDs in <c>[#chunkId]</c> form. Projects the candidates into
/// <see cref="Models.Citation"/> records on the output payload.
/// </summary>
public sealed class AnswerComposer : Executor<PipelineStepContext<RerankOutput>, PipelineStepContext<RagPipelineOutput>>
{
    private readonly AIAgent _agent;
    private readonly AgentsOptions _options;
    private readonly string _model;

    /// <summary>Creates a new <see cref="AnswerComposer"/>.</summary>
    public AnswerComposer(IAgentsFactory agents, IOptions<AgentsOptions> options) : base("AnswerComposer") {
        _options = options.Value;
        _model = _options.AzureOpenAI.Deployments.Reasoning!;
        _agent = agents.Create(new AgentDescriptor {
            Name = "DexAnswerComposer",
            Role = AgentModelRole.Reasoning,
            PromptTemplate = "AnswerComposer",
            PromptValues = new Dictionary<string, object?> { ["strictGrounding"] = _options.Pipeline.StrictGrounding },
            IncludeUserContext = true,
        });
    }

    /// <inheritdoc/>
    public override async ValueTask<PipelineStepContext<RagPipelineOutput>> HandleAsync(PipelineStepContext<RerankOutput> envelope,
        IWorkflowContext context, CancellationToken cancellationToken = default) {

        var candidates = envelope.Payload.RerankedCandidates;
        var prompt = BuildPrompt(envelope.State.Question, candidates);
        var agentSession = await _agent.CreateSessionAsync(cancellationToken);
        SessionStoreChatHistoryProvider.SetSessionId(agentSession, envelope.State.SessionId);

        // Stream the answer: emit each text delta as a workflow event (surfaced as an SSE `delta` by the
        // streaming runner; ignored by the non-streaming runner) while accumulating the full text. Token
        // usage arrives as trailing UsageContent on the final update(s); fold it and report it once.
        var answer = new StringBuilder();
        UsageDetails? usage = null;
        await foreach (var update in _agent.RunStreamingAsync(prompt, agentSession, cancellationToken: cancellationToken)) {
            if (!string.IsNullOrEmpty(update.Text)) {
                answer.Append(update.Text);
                await context.AddEventAsync(new AnswerDeltaEvent(update.Text), cancellationToken);
            }
            foreach (var usageContent in update.Contents.OfType<UsageContent>()) {
                (usage ??= new UsageDetails()).Add(usageContent.Details);
            }
        }
        if (usage is not null) {
            await context.AddEventAsync(new UsageEvent(usage, _model), cancellationToken);
        }

        var citations = candidates
            .Select((c, index) => new Models.Citation {
                ChunkId = c.Id,
                DocumentId = c.Source.Id,
                Title = c.Title,
                HeadingPath = c.HeadingPath,
                Score = c.Score,
                Number = index + 1,
            })
            .ToList();
        var sources = candidates.Select(c => c.Source).DistinctBy(x => x.Id).ToList();
        return envelope.Next(new RagPipelineOutput {
            Answer = answer.ToString(),
            Citations = citations,
            Sources = sources,
        });
    }

    private static string BuildPrompt(string question, IReadOnlyList<RetrievedChunk> candidates) {
        var sb = new StringBuilder();
        sb.AppendLine("CONTEXT:");
        if (candidates.Count == 0) {
            sb.AppendLine("(no candidates retrieved)");
        }
        for (var i = 0; i < candidates.Count; i++) {
            var c = candidates[i];
            sb.Append($"[{i + 1}](#{c.Id}) ");
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
