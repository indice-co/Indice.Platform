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
    private readonly SystemOneDecisionService _systemOne;
    private readonly AgentMessageLocalizer _localizer;

    /// <summary>Creates a new <see cref="IntentRouterService"/>.</summary>
    public IntentRouterService([FromKeyedServices(nameof(AgentsOptions.AzureOpenAIDeployments.Reasoning))] IChatClient chatClient, IOptions<AgentsOptions> agentsOptions, IOptions<ModelsOptions> models, IPromptTemplateRenderer prompts,
        AgentInfoRegistry registry, UserClaimsAIContextProvider userClaimsProvider, ConversationStoreChatHistoryProvider historyProvider,
        SystemOneDecisionService systemOne, AgentMessageLocalizer localizer) {
        _registry = registry;
        _routing = agentsOptions.Value.Routing;
        _systemOne = systemOne;
        _localizer = localizer;
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
            var selected = _registry.Find(suggestedAgent);
            if (selected is null) {
                return new RouteDecision {
                    Reason = $"Agent '{suggestedAgent}' is not registered.",
                    HasError = true
                };
            }
            return new RouteDecision {
                AgentName = selected.Name,
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

            // System-one first; null means it is off, failed or unsure — the Reasoning-model agent below decides instead.
            var systemOneDecision = await RouteWithSystemOneAsync(message, chatOptions.ConversationId, cancellationToken);
            if (systemOneDecision is not null) {
                return systemOneDecision;
            }

            var response = await _agent.RunAsync<RouteDecision>(message.Text, session, cancellationToken: cancellationToken);
            var result = response.Result;
            var agent = result.IsInScope ? _registry.Find(result.AgentName ?? string.Empty) : null;
            return new RouteDecision {
                AgentName = agent?.Name,
                Reason = result.Reason,
                IsInScope = agent is not null
            };
        } catch (Exception exception) when (exception is not OperationCanceledException) {
            return new RouteDecision {
                Reason = exception.Message,
                IsInScope = false,
                HasError = true
            };
        }
    }
    private async Task<RouteDecision?> RouteWithSystemOneAsync(ChatMessage message, string? conversationId, CancellationToken cancellationToken) {
        if (!_systemOne.IsEnabled) {
            return null;
        }
        var targets = _registry.RoutableTargets();
        if (targets.Count == 0) {
            return null;
        }
        var decision = await _systemOne.DecideAsync(BuildSystemOneRequest(message, conversationId, targets), cancellationToken);
        return decision is null ? null : ToRouteDecision(decision);
    }

    private static SystemOneDecisionRequest BuildSystemOneRequest(ChatMessage message, string? conversationId, IReadOnlyList<AgentInfo> targets) =>
        new() {
            ConversationId = Guid.TryParse(conversationId, out var id) ? id : null,
            Message = message.Text,
            ContextPrompt = nameof(AgentsConstants.PromptDefaults.IntentRouterSystemOne),
            Questions = {
                [InScopeQuestion] = SystemOneQuestion.Noul("Can one of the assistant's agents handle the latest message?", required: true),
                [AgentQuestion] = SystemOneQuestion.Choice("Which agent should handle the latest message?",
                    targets.ToDictionary(agent => agent.Name, agent => agent.Description)),
            }
        };

    private RouteDecision? ToRouteDecision(SystemOneDecision decision) {
        if (!decision.Get(InScopeQuestion)!.IsTrue) {
            return new RouteDecision {
                Reason = _localizer.OutOfScopeReply,
                IsInScope = false
            };
        }
        // The agent only matters for an in-scope message (no agent fits an out-of-scope one); there an unsure one goes to the LLM.
        var agentAnswer = decision.Get(AgentQuestion);
        if (agentAnswer is not { IsConfident: true }) {
            return null;
        }
        var agent = _registry.Find(agentAnswer.Choice!);
        return agent is null ? null : new RouteDecision {
            AgentName = agent.Name,
            IsInScope = true
        };
    }

    private const string InScopeQuestion = "in_scope";
    private const string AgentQuestion = "agent";

    private static bool TryGetChatTopic(ChatMessage message, out ChatTopic? topic) {
        topic = null;
        return message.AdditionalProperties?.TryGetValue("ChatTopic", out topic) == true
            && topic is not null
            && !string.IsNullOrWhiteSpace(topic.ReferenceId);
    }
}
