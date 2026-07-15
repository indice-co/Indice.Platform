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
/// Terminal branch of the pipeline when <c>IntentClassifier</c> decides the \
/// question is a general question about the capabilities of the agent.
/// </summary>
internal class PurposeResponder : Executor<PipelineStepContext<IntentOutput>, PipelineStepContext<RagPipelineOutput>>
{
    private readonly AIAgent _agent;
    private readonly AgentsOptions _options;
    private readonly string _model;


    /// <summary>Creates a new <see cref="PurposeResponder"/>.</summary>
    public PurposeResponder(IAgentsFactory agents, IOptions<AgentsOptions> options) : base("PurposeResponder") {
        _options = options.Value;
        _model = _options.AzureOpenAI.Deployments.Reasoning!;
        _agent = agents.Create(new AgentDescriptor {
            Name = "DexPurposeResponder",
            Role = AgentModelRole.Reasoning,
            PromptTemplate = "PurposeResponder",
            PromptValues = new Dictionary<string, object?> { ["strictGrounding"] = _options.Pipeline.StrictGrounding },
            IncludeUserContext = true,
        });
    }

    /// <inheritdoc/>
    public override async ValueTask<PipelineStepContext<RagPipelineOutput>> HandleAsync(
        PipelineStepContext<IntentOutput> envelope,IWorkflowContext context,
        CancellationToken cancellationToken = default) {

        var prompt = envelope.State.Question;
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

        return envelope.Next(new RagPipelineOutput {
            Answer = answer.ToString()
        });
    }
}


