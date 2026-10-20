using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using static Duende.IdentityModel.ClaimComparer;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Master intent router. Classifies the latest user message against the <see cref="AgentInfoRegistry"/> of
/// available agents and returns the name of the agent that should handle it, or an out-of-scope decision
/// when no registered agent fits. It asks the system-one decision model (<see cref="ISystemOneClient"/>) first when
/// one is configured, and falls back to one Reasoning-model call when that model is off, fails or is unsure. It mirrors
/// <c>IntentClassifier</c>, but routes <em>across</em> agents rather than classifying intent <em>within</em> the knowledge pipeline.
/// </summary>
public class IntentRouterService
{
    private readonly AIAgent _agent;
    private readonly AgentInfoRegistry _registry;

    private readonly AgentsOptions.RoutingOptions _routing;
    private readonly ISystemOneClient _systemOne;
    private readonly IConversationStore _store;
    private readonly AgentMessageLocalizer _localizer;
    private readonly ILogger<IntentRouterService> _logger;
    private readonly bool _systemOneEnabled;
    private readonly double _minConfidence;
    private readonly string _systemOneContext;

    /// <summary>Creates a new <see cref="IntentRouterService"/>.</summary>
    public IntentRouterService([FromKeyedServices(nameof(AgentsOptions.AzureOpenAIDeployments.Reasoning))] IChatClient chatClient, IOptions<AgentsOptions> agentsOptions, IOptions<ModelsOptions> models, IPromptTemplateRenderer prompts,
        AgentInfoRegistry registry, UserClaimsAIContextProvider userClaimsProvider, ConversationStoreChatHistoryProvider historyProvider,
        ISystemOneClient systemOne, IConversationStore store, AgentMessageLocalizer localizer, ILogger<IntentRouterService> logger) {
        _registry = registry;
        _routing = agentsOptions.Value.Routing;
        _systemOne = systemOne;
        _store = store;
        _localizer = localizer;
        _logger = logger;
        _systemOneEnabled = agentsOptions.Value.SystemOne.Enabled && !string.IsNullOrWhiteSpace(agentsOptions.Value.AzureOpenAI.Deployments.SystemOne);
        _minConfidence = agentsOptions.Value.SystemOne.MinConfidence;
        var routerModel = new {
            agents = registry.RoutableTargets().Select(agent => new { name = agent.Name, description = agent.Description }),
        };
        _systemOneContext = prompts.Render(nameof(AgentsConstants.PromptDefaults.IntentRouterSystemOne));
        var chatOptions = models.Value.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = prompts.Render("IntentRouter", routerModel);
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
        if (_registry.RoutableTargets().Any(x => x.Name == AgentsConstants.AgentNames.Operator) && TryGetChatTopic(message, out var topic)) {
            return new RouteDecision {
                AgentName = AgentsConstants.AgentNames.Operator,
                IsInScope = true
            };
        }
        // System-one first; null means it is off, failed or unsure — the Reasoning-model agent decides instead.
        return await RouteWithSystemOneAsync(message, chatOptions.ConversationId, cancellationToken)
            ?? await RouteWithLlmAsync(message, chatOptions.ConversationId, cancellationToken);
    }

    private async Task<RouteDecision> RouteWithLlmAsync(ChatMessage message, string? conversationId, CancellationToken cancellationToken) {
        try {
            var session = await _agent.CreateSessionAsync(cancellationToken);
            if (conversationId is not null) {
                ConversationStoreChatHistoryProvider.SetSessionId(session, Guid.Parse(conversationId));
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
        if (!_systemOneEnabled) {
            return null;
        }
        if (string.IsNullOrWhiteSpace(message.Text)) {
            return null;
        }
        SystemOneDecision decision;
        try {
            var history = Guid.TryParse(conversationId, out var id) && id != Guid.Empty
                ? await _store.GetHistoryAsync(id, cancellationToken)
                : [];
            var turns = history.Where(turn => !string.IsNullOrWhiteSpace(turn.Text))
                .Select(turn => new SystemOneTurn { Role = turn.Role.Value, Text = turn.Text })
                .ToList();
            decision = await _systemOne.DecideAsync(new SystemOneDecisionRequest {
                State = new SystemOneState {
                    Context = string.IsNullOrWhiteSpace(_systemOneContext) ? null : _systemOneContext.Trim(),
                    History = turns.Count > 0 ? turns : null,
                    Message = message.Text,
                },
                Questions = {
                    [InScopeQuestion] = SystemOneQuestion.Noul("Can one of the assistant's agents handle the latest message?",
                        whenTrue: "One of the agents can handle it, including a follow-up to an earlier turn",
                        whenFalse: "None of the agents can handle the message, including any follow-up to an earlier turn"),
                    [AgentQuestion] = SystemOneQuestion.Choice("Which agent should handle the latest message? A follow-up belongs to the agent that handled the previous turn.",
                        // The option text of an agent: its description, capabilities and domains, as registered in its AgentInfo.
                        _registry.RoutableTargets().ToDictionary(agent => agent.Name, agent => string.Concat(
                            agent.Description,
                            agent.Capabilities is { Count: > 0 } capabilities ? $" Capabilities: {string.Join("; ", capabilities.Select(capability => $"{capability.Name} - {capability.Description.TrimEnd('.')}"))}." : null,
                            agent.Domains is { Count: > 0 } domains ? $" Domains: {string.Join(", ", domains)}." : null))),
                }
            }, cancellationToken);
        } catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested) {
            _logger.LogWarning(exception, "System-one routing failed; falling back to the LLM.");
            return null;
        }
        var inScopeAnswer = decision.Get(InScopeQuestion);
        if (inScopeAnswer is null || inScopeAnswer.Confidence < _minConfidence) {
            _logger.LogDebug("System-one is unsure whether the message is in scope (confidence {Confidence}); falling back to the LLM.", inScopeAnswer?.Confidence);
            return null;
        }
        if (!inScopeAnswer.IsTrue) {
            return new RouteDecision {
                Reason = _localizer.OutOfScopeReply,
                IsInScope = false
            };
        }
        // The agent only matters for an in-scope message (no agent fits an out-of-scope one); there an unsure one goes to the LLM.
        var agentAnswer = decision.Get(AgentQuestion);
        if (agentAnswer?.Choice is null || agentAnswer.Confidence < _minConfidence) {
            _logger.LogDebug("System-one is unsure about the agent (choice {Choice}, confidence {Confidence}); falling back to the LLM.", agentAnswer?.Choice, agentAnswer?.Confidence);
            return null;
        }
        var agent = _registry.Find(agentAnswer.Choice);
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
