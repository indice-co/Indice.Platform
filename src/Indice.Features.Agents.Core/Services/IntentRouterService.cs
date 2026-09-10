using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Master intent router. Classifies the latest user message against the <see cref="AgentInfoRegistry"/> of
/// available agents and returns the name of the agent that should handle it, or an out-of-scope decision
/// when no registered agent fits. One Reasoning-model call — it mirrors <c>IntentClassifier</c>, but routes
/// <em>across</em> agents rather than classifying intent <em>within</em> the knowledge pipeline.
/// </summary>
public class IntentRouterService
{
    private readonly AIAgent _agent;
    private readonly AgentInfoRegistry _registry;

    /// <summary>Creates a new <see cref="IntentRouterService"/>.</summary>
    public IntentRouterService([FromKeyedServices(nameof(AgentsOptions.AzureOpenAIDeployments.Reasoning))] IChatClient chatClient, IOptions<ModelsOptions> models, IPromptTemplateRenderer prompts,
        AgentInfoRegistry registry, UserClaimsAIContextProvider userClaimsProvider, ConversationStoreChatHistoryProvider historyProvider) {
        _registry = registry;
        var chatOptions = models.Value.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = prompts.Render("IntentRouter", new {
            agents = registry.RoutableTargets().Select(agent => new { name = agent.Name, description = agent.Description }),
        });
        _agent = chatClient.AsAIAgent(
            options: new ChatClientAgentOptions() {
                ChatOptions = chatOptions,
                AIContextProviders = [userClaimsProvider],
                Name = "DexIntentRouter",
                ChatHistoryProvider = historyProvider,
                // Chat completions is stateless; the M.E.AI OpenAI client echoes the request ConversationId onto
                // the response, which the agent would otherwise misread as server-side history and throw.
                ThrowOnChatHistoryProviderConflict = false
            });
    }

    /// <summary>Routes the latest user message to the agent that should handle it.</summary>
    /// <param name="question">The latest user message text.</param>
    /// <param name="conversationId">The conversation id, used to load recent history so follow-ups route in context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A <see cref="RouteDecision"/> whose <see cref="RouteDecision.AgentName"/> is the chosen agent, or
    /// <c>null</c> with a populated <see cref="RouteDecision.Reason"/> when the request is out of scope.
    /// </returns>
    public async Task<RouteDecision> RouteAsync(string question, string conversationId, CancellationToken cancellationToken = default) {
        var session = await _agent.CreateSessionAsync(cancellationToken);
        ConversationStoreChatHistoryProvider.SetSessionId(session, Guid.Parse(conversationId));
        var response = await _agent.RunAsync<RouteDecision>(question, session, cancellationToken: cancellationToken);
        var result = response.Result;
        return new RouteDecision {
            AgentName = result.IsInScope && _registry.Find(result.AgentName ?? string.Empty) is not null ? result.AgentName : null,
            Reason = result.Reason,
            IsInScope = result.IsInScope
        };
    }
}
