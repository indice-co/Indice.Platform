using System.Security.Claims;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Providers;
using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Master intent router. Classifies the latest user message against the <see cref="AgentInfoRegistry"/> of
/// available agents and returns the name of the agent that should handle it, or an out-of-scope decision
/// when no registered agent fits. It asks the system-one decision model (<see cref="ISystemOneClient"/>) first when
/// one is configured, and falls back to one Reasoning-model call when that model is off, fails or is unsure. It mirrors
/// <c>IntentClassifier</c>, but routes <em>across</em> agents rather than classifying intent <em>within</em> the knowledge pipeline.
/// </summary>
/// <remarks>
/// Routing is per caller: an agent that declares <see cref="AgentInfo.RequiredRoles"/> is neither offered to the models nor
/// selectable by name for a caller without one of those roles.
/// </remarks>
public class IntentRouterService
{
    private readonly IChatClient _chatClient;
    private readonly ModelsOptions _models;
    private readonly IPromptTemplateRenderer _prompts;
    private readonly UserClaimsAIContextProvider _userClaimsProvider;
    private readonly ConversationStoreChatHistoryProvider _historyProvider;
    private readonly AgentInfoRegistry _registry;
    private readonly AgentsClaimsPrincipalSelector _claimsPrincipalSelector;
    private readonly IKnowledgeHintService _knowledgeHints;

    private readonly AgentsOptions.RoutingOptions _routing;
    private readonly ISystemOneClient _systemOne;
    private readonly IConversationStore _store;
    private readonly PersistedCheckpointStore _checkpoints;
    private readonly AgentMessageLocalizer _localizer;
    private readonly ILogger<IntentRouterService> _logger;
    private readonly bool _systemOneEnabled;
    private readonly double _minConfidence;
    private readonly string _systemOneContext;

    /// <summary>Creates a new <see cref="IntentRouterService"/>.</summary>
    public IntentRouterService([FromKeyedServices(nameof(AgentsModelRole.Reasoning))] IChatClient chatClient, IOptions<AgentsOptions> agentsOptions, IOptions<ModelsOptions> models, IPromptTemplateRenderer prompts,
        AgentInfoRegistry registry, UserClaimsAIContextProvider userClaimsProvider, ConversationStoreChatHistoryProvider historyProvider, AgentsClaimsPrincipalSelector claimsPrincipalSelector,
        IKnowledgeHintService knowledgeHints, ISystemOneClient systemOne, IConversationStore store, PersistedCheckpointStore checkpoints, AgentMessageLocalizer localizer, ILogger<IntentRouterService> logger) {
        _knowledgeHints = knowledgeHints;
        _chatClient = chatClient;
        _models = models.Value;
        _prompts = prompts;
        _userClaimsProvider = userClaimsProvider;
        _historyProvider = historyProvider;
        _registry = registry;
        _claimsPrincipalSelector = claimsPrincipalSelector;
        _checkpoints = checkpoints;
        _routing = agentsOptions.Value.Routing;
        _systemOne = systemOne;
        _store = store;
        _localizer = localizer;
        _logger = logger;
        _systemOneEnabled = agentsOptions.Value.SystemOne.Enabled && !string.IsNullOrWhiteSpace(agentsOptions.Value.AzureOpenAI.Deployments.SystemOne);
        _minConfidence = agentsOptions.Value.SystemOne.MinConfidence;
        _systemOneContext = prompts.Render(nameof(AgentsConstants.PromptDefaults.IntentRouterSystemOne));
    }

    /// <summary>Routes the latest user message to the agent that should handle it.</summary>
    /// <remarks>
    /// A workflow waiting for user input keeps the turn: a reply to a port request, or a plain-text message while a port
    /// request is still pending (coerced into the port's response), goes to the agent owning the request's checkpoint
    /// without classification. Only an explicit agent selection takes precedence. Every path only considers the agents the
    /// current caller is allowed to use.
    /// </remarks>
    /// <param name="message">The latest user message.</param>
    /// <param name="chatOptions">The chat options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A <see cref="RouteDecision"/> whose <see cref="RouteDecision.AgentName"/> is the chosen agent, or
    /// <c>null</c> with a populated <see cref="RouteDecision.Reason"/> when the request is out of scope.
    /// </returns>
    public async Task<RouteDecision> RouteAsync(ChatMessage message, ChatOptions chatOptions, CancellationToken cancellationToken = default) {
        var user = _claimsPrincipalSelector();
        if (await TryRouteToActiveWorkflowAsync(message, chatOptions.ConversationId, user, cancellationToken) is { } pinned) {
            return pinned;
        }
        var suggestedAgent = chatOptions.Instructions;

        suggestedAgent = suggestedAgent?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(suggestedAgent)) {
            suggestedAgent = _routing.DefaultAgent?.Trim().ToLowerInvariant();
        }
        if (!string.Equals(suggestedAgent, AgentsConstants.AgentNames.Auto, StringComparison.OrdinalIgnoreCase)) {
            suggestedAgent = string.IsNullOrWhiteSpace(suggestedAgent) ? AgentsConstants.AgentNames.Knowledge : suggestedAgent;
            // An agent the caller may not use answers exactly like one that is not registered, so names cannot be probed.
            var selected = _registry.Find(suggestedAgent, user);
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
        var targets = _registry.RoutableTargets(user);
        if (targets.Any(x => x.Name == AgentsConstants.AgentNames.Operator) && TryGetChatTopic(message, out var topic)) {
            return new RouteDecision {
                AgentName = AgentsConstants.AgentNames.Operator,
                IsInScope = true
            };
        }
        // System-one first; null means it is off, failed or unsure — the Reasoning-model agent decides instead.
        return await RouteWithSystemOneAsync(message, chatOptions.ConversationId, targets, cancellationToken)
            ?? await RouteWithLlmAsync(message, chatOptions.ConversationId, targets, cancellationToken);
    }

    private async Task<RouteDecision> RouteWithLlmAsync(ChatMessage message, string? conversationId, IReadOnlyList<AgentInfo> targets, CancellationToken cancellationToken) {
        try {
            var agent = CreateRouterAgent(targets);
            var session = await agent.CreateSessionAsync(cancellationToken);
            if (conversationId is not null) {
                ConversationStoreChatHistoryProvider.SetSessionId(session, Guid.Parse(conversationId));
            }
            var prompt = AppendHints(message.Text, await GetHintsAsync(message.Text, targets, cancellationToken));
            var response = await agent.RunAsync<RouteDecision>(prompt, session, cancellationToken: cancellationToken);
            var result = response.Result;
            // Only an agent the model was offered may come back: a name it made up, or one filtered out for this caller, is out of scope.
            var chosen = result.IsInScope ? targets.FirstOrDefault(target => string.Equals(target.Name, result.AgentName, StringComparison.OrdinalIgnoreCase)) : null;
            return new RouteDecision {
                AgentName = chosen?.Name,
                Reason = result.Reason,
                IsInScope = chosen is not null
            };
        } catch (Exception exception) when (exception is not OperationCanceledException) {
            return new RouteDecision {
                Reason = exception.Message,
                IsInScope = false,
                HasError = true
            };
        }
    }

    // A failing hint lookup (embedding service down, search error) must never stop routing: the router then decides without it.
    private async Task<IReadOnlyList<KnowledgeHint>> GetHintsAsync(string? text, IReadOnlyList<AgentInfo> targets, CancellationToken cancellationToken) {
        try {
            return await _knowledgeHints.GetHintsAsync(text ?? string.Empty, targets, cancellationToken);
        } catch (Exception exception) when (exception is not OperationCanceledException) {
            _logger.LogWarning(exception, "Knowledge hints are unavailable; routing without them.");
            return [];
        }
    }

    private static string AppendHints(string? text, IReadOnlyList<KnowledgeHint> hints) {
        if (hints.Count == 0) {
            return text ?? string.Empty;
        }
        var lines = hints.Select(hint => string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"- {hint.Category} (score {hint.Score:0.00}) -> agent {string.Join(", ", hint.AgentNames)}"));
        return $"{text}\n\nKNOWLEDGE MATCHES:\n{string.Join('\n', lines)}";
    }

    /// <summary>
    /// Builds the router's Reasoning-model agent for one call. The instructions list only the agents the caller may use, so
    /// they are rendered per call rather than once for the whole application.
    /// </summary>
    private AIAgent CreateRouterAgent(IReadOnlyList<AgentInfo> targets) {
        var routerModel = new {
            agents = targets.Select(agent => new { name = agent.Name, description = agent.Description }),
        };
        var chatOptions = _models.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = _prompts.Render("IntentRouter", routerModel);
        return _chatClient.AsAIAgent(
            options: new ChatClientAgentOptions() {
                ChatOptions = chatOptions,
                AIContextProviders = [_userClaimsProvider],
                Name = "DexIntentRouter",
                ChatHistoryProvider = _historyProvider,
                // Chat completions is stateless; the M.E.AI OpenAI client echoes the request ConversationId onto
                // the response, which the agent would otherwise misread as server-side history and throw.
                ThrowOnChatHistoryProviderConflict = false
            });
    }

    private async Task<RouteDecision?> RouteWithSystemOneAsync(ChatMessage message, string? conversationId, IReadOnlyList<AgentInfo> targets, CancellationToken cancellationToken) {
        if (!_systemOneEnabled || targets.Count == 0) {
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
                        targets.ToDictionary(agent => agent.Name, agent => string.Concat(
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
        var agent = targets.FirstOrDefault(target => string.Equals(target.Name, agentAnswer.Choice, StringComparison.OrdinalIgnoreCase));
        return agent is null ? null : new RouteDecision {
            AgentName = agent.Name,
            IsInScope = true
        };
    }

    /// <summary>
    /// Keeps the user inside a workflow that is waiting for input. A reply to a port request (function result) goes to the
    /// agent that owns the request's checkpoint; a plain-text reply while a port request is still pending goes there too,
    /// coerced into the port's response so the run resumes from the checkpoint instead of being re-classified.
    /// </summary>
    private async Task<RouteDecision?> TryRouteToActiveWorkflowAsync(ChatMessage message, string? conversationId, ClaimsPrincipal? user, CancellationToken cancellationToken) {
        if (!Guid.TryParse(conversationId, out var id) || id == Guid.Empty) {
            return null;
        }
        if (message.HasFunctionResultContent()) {
            var callId = message.GetFunctionResultContentCallId();
            return await PinAsync(id, callId.CheckpointId, user, cancellationToken);
        }
        var history = await _store.GetHistoryAsync(id, cancellationToken);
        if (!history.TryGetPendingPortRequest(out var pendingRequest)) {
            return null;
        }
        var decision = await PinAsync(id, AgentFunctionCallId.Parse(pendingRequest.CallId).CheckpointId, user, cancellationToken);
        if (decision is not null && PortResponseCoercer.TryCoerce(pendingRequest, message.Text, out var result)) {
            message.Contents.Insert(0, result!);
        }
        return decision;
    }

    private async Task<RouteDecision?> PinAsync(Guid conversationId, string? checkpointId, ClaimsPrincipal? user, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(checkpointId)) {
            return null;
        }
        var session = await _checkpoints.FindSessionByCheckpointIdAsync(conversationId, checkpointId, cancellationToken);
        // A caller who lost access to the agent (role removed mid-conversation) is no longer pinned to its workflow.
        var agent = session?.AgentName is { } agentName ? _registry.Find(agentName, user) : null;
        if (agent is null) {
            return null;
        }
        _logger.LogDebug("Workflow of agent {AgentName} is waiting for input (checkpoint {CheckpointId}); pinning the turn to it.", agent.Name, checkpointId);
        return new RouteDecision {
            AgentName = agent.Name,
            Reason = "Workflow in progress",
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
