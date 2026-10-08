using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using static Duende.IdentityModel.ClaimComparer;

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

    private readonly AgentsOptions.RoutingOptions _routing;

    /// <summary>Creates a new <see cref="IntentRouterService"/>.</summary>
    public IntentRouterService([FromKeyedServices(nameof(AgentsOptions.AzureOpenAIDeployments.Reasoning))] IChatClient chatClient, IOptions<AgentsOptions> agentsOptions, IOptions<ModelsOptions> models, IPromptTemplateRenderer prompts,
        AgentInfoRegistry registry, UserClaimsAIContextProvider userClaimsProvider, ConversationStoreChatHistoryProvider historyProvider) {
        _registry = registry;
        _routing = agentsOptions.Value.Routing;
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
    /// <param name="message">The latest user message.</param>
    /// <param name="chatOptions">The chat options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A <see cref="RouteDecision"/> whose <see cref="RouteDecision.AgentName"/> is the chosen agent, or
    /// <c>null</c> with a populated <see cref="RouteDecision.Reason"/> when the request is out of scope.
    /// </returns>
    public async Task<RouteDecision> RouteAsync(ChatMessage message, ChatOptions chatOptions, CancellationToken cancellationToken = default) {
        var suggestedAgent = chatOptions.Instructions;

        suggestedAgent = suggestedAgent?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(suggestedAgent)) {
            suggestedAgent = _routing.DefaultAgent?.Trim().ToLowerInvariant();
        }
        if (!string.Equals(suggestedAgent, AgentsConstants.AgentNames.Auto, StringComparison.OrdinalIgnoreCase)) {
            suggestedAgent = string.IsNullOrWhiteSpace(suggestedAgent) ? AgentsConstants.AgentNames.Knowledge : suggestedAgent;
            return new RouteDecision {
                AgentName = suggestedAgent,
                Reason = "User selection",
                IsInScope = true,
            };
        }
        try {
            var session = await _agent.CreateSessionAsync(cancellationToken);
            if (chatOptions.ConversationId is not null) { 
                ConversationStoreChatHistoryProvider.SetSessionId(session, Guid.Parse(chatOptions.ConversationId!));
            }
            if (_registry.RoutableTargets().Any(x => x.Name == AgentsConstants.AgentNames.Operator) && TryGetChatTopic(message, out var topic)) {
                return new RouteDecision {
                    AgentName = AgentsConstants.AgentNames.Operator,
                    IsInScope = true
                };
            }

            var response = await _agent.RunAsync<RouteDecision>(message.Text, session, cancellationToken: cancellationToken);
            var result = response.Result;
            return new RouteDecision {
                AgentName = result.IsInScope && _registry.Find(result.AgentName ?? string.Empty) is not null ? result.AgentName : null,
                Reason = result.Reason,
                IsInScope = result.IsInScope
            };
        } catch (Exception exception) when (exception is not OperationCanceledException) {
            return new RouteDecision {
                Reason = exception.Message,
                IsInScope = false,
                HasError = true
            };
        }
    }
    private static bool TryGetChatTopic(ChatMessage message, out ChatTopic? topic) {
        topic = null;
        return message.AdditionalProperties?.TryGetValue("ChatTopic", out topic) == true
            && topic is not null
            && !string.IsNullOrWhiteSpace(topic.ReferenceId);
    }
}
