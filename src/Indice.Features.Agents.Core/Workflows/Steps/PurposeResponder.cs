using System.Text;
using Indice.Features.Agents.Core.Workflows.Events;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps;

/// <summary>
/// Terminal branch of the pipeline when <c>IntentClassifier</c> decides the \
/// question is a general question about the capabilities of the agent.
/// </summary>
internal class PurposeResponder : Executor<IntentOutput, GroundedAnswerOutput>
{
    private readonly AIAgent _agent;
    private readonly AgentsOptions _options;
    private readonly string _model;


    /// <summary>Creates a new <see cref="PurposeResponder"/>.</summary>
    public PurposeResponder(
        [FromKeyedServices(nameof(AgentsOptions.AzureOpenAIDeployments.Reasoning))] IChatClient chatClient, 
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models, IPromptTemplateRenderer prompts,
        UserClaimsAIContextProvider userClaimsProvider,
        ConversationStoreChatHistoryProvider historyProvider) : base("PurposeResponder") {
        _options = options.Value;
        _model = _options.AzureOpenAI.Deployments.Reasoning!;

        var chatOptions = models.Value.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = prompts.Render("PurposeResponder", new {
            strictGrounding = _options.Pipeline.StrictGrounding,
        });

        _agent = chatClient
            .AsAIAgent(
                options: new ChatClientAgentOptions() {
                    ChatOptions = chatOptions,
                    AIContextProviders = [userClaimsProvider],
                    Name = "DexPurposeResponder",
                    ChatHistoryProvider = historyProvider,
                    // Chat completions is stateless; the echoed request ConversationId must not be treated as server-side history.
                    ThrowOnChatHistoryProviderConflict = false,
                });
    }

    /// <inheritdoc/>
    public override async ValueTask<GroundedAnswerOutput> HandleAsync(
        IntentOutput intentResult, IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        var state = await context.GetConversationStateAsync(cancellationToken);
        var prompt = state.Message.Text;
        var agentSession = await _agent.CreateSessionAsync(cancellationToken);
        ConversationStoreChatHistoryProvider.SetSessionId(agentSession, Guid.Parse(state.ConversationId));

        // Stream the answer: emit each text delta as a workflow event (surfaced as an SSE `delta` by the
        // streaming runner; ignored by the non-streaming runner) while accumulating the full text. Token
        // usage arrives as trailing UsageContent on the final update(s); fold it and report it once.
        var answer = new StringBuilder();
        await foreach (var update in _agent.RunStreamingAsync(prompt, agentSession, cancellationToken: cancellationToken)) {
            if (!string.IsNullOrEmpty(update.Text)) {
                answer.Append(update.Text);
            }
            await context.AddEventAsync(new AgentResponseUpdateEvent(Id, update), cancellationToken);
        }

        return new GroundedAnswerOutput(answer.ToString(), [], []);
    }
}


