using System.Runtime.CompilerServices;
using Indice.Features.Agents.Core.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core;

/// <summary>Entry point for executing a chat turn against one of the registered agents.</summary>
public interface IDexChatClient : IChatClient
{
}

/// <summary>
/// <see cref="IChatClient"/> facade over the Agents catalogue. Resolves the requested agent (<see cref="ChatOptions.Instructions"/>
/// carries the <c>agentName</c> from the HTTP layer), runs it through the Agent Framework agent surface and projects its streaming
/// updates onto <see cref="ChatResponseUpdate"/>s. Workflows reach here already wrapped as agents (see
/// <c>AddAgentWorkflow</c>), so one code path serves the intent router, the knowledge pipeline and any custom agent.
/// </summary>
public class AgentsChatClient : IDexChatClient
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IReadOnlyDictionary<string, AgentDefinition> _agents;
    private readonly AgentsOptions.RoutingOptions _routing;
    private readonly ILogger<AgentsChatClient> _logger;

    /// <summary>Creates a new <see cref="AgentsChatClient"/>.</summary>
    /// <param name="serviceProvider">The request-scoped service provider handed to agent factories.</param>
    /// <param name="agents">The registered agents.</param>
    /// <param name="options">Agents options; <see cref="AgentsOptions.Routing"/> decides the fallback agent.</param>
    /// <param name="logger">The logger.</param>
    public AgentsChatClient(IServiceProvider serviceProvider, IEnumerable<AgentDefinition> agents, IOptions<AgentsOptions> options, ILogger<AgentsChatClient> logger) {
        _serviceProvider = serviceProvider;
        _agents = agents.ToDictionary(definition => definition.Name, StringComparer.OrdinalIgnoreCase);
        _routing = options.Value.Routing;
        _logger = logger;
    }

    /// <summary>
    /// Picks the agent to run: the requested name when it is registered, else the configured default agent when registered,
    /// else <see cref="AgentsConstants.AgentNames.Knowledge"/>. Matching is case-insensitive; the registered spelling is returned.
    /// </summary>
    public static string ResolveAgentName(string? requested, IEnumerable<string> registered, string? defaultAgent) {
        ArgumentNullException.ThrowIfNull(registered);
        var names = registered as ICollection<string> ?? registered.ToList();
        var wanted = requested?.Trim();
        if (!string.IsNullOrEmpty(wanted) && names.FirstOrDefault(name => name.Equals(wanted, StringComparison.OrdinalIgnoreCase)) is { } match) {
            return match;
        }
        var fallback = defaultAgent?.Trim();
        if (!string.IsNullOrEmpty(fallback) && names.FirstOrDefault(name => name.Equals(fallback, StringComparison.OrdinalIgnoreCase)) is { } defaultMatch) {
            return defaultMatch;
        }
        return AgentsConstants.AgentNames.Knowledge;
    }

    /// <inheritdoc/>
    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) {
        var stream = GetStreamingResponseAsync(messages, options, cancellationToken);
        var response = await stream.ToChatResponseAsync();
        // Step progress contents are ephemeral (streaming UI only) and must not survive in the composed response.
        foreach (var message in response.Messages) {
            message.Contents = message.Contents.Where(content => content is not StepProgressContent).ToList();
        }
        return response;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        if (messages.Count() != 1) {
            throw new ArgumentException("DexChatClient only supports a single user message per request. No batching allowed.", nameof(messages));
        }
        var message = messages.First();
        var conversationId = Guid.TryParse(options?.ConversationId, out var parsed) ? parsed : Guid.NewGuid();
        var conversationIdText = conversationId.ToString();
        // Agents receive the message through the framework without our session or workflow state; the id rides on the message.
        message.SetConversationId(conversationId);
        // Dex models a turn as one assistant message (the store persists the first message, the stream projects into
        // /messages/0). Framework-hosted agents stamp their own message ids per hop, which would split the aggregated
        // response into several messages, so every update of the turn is re-stamped with one message/response id.
        var turnMessageId = Guid.NewGuid().ToString("N");
        var turnResponseId = Guid.NewGuid().ToString("N");

        var agentName = ResolveAgentName(options?.Instructions, _agents.Keys, _routing.DefaultAgent);
        if (!_agents.TryGetValue(agentName, out var definition)) {
            throw new InvalidOperationException($"Agent '{agentName}' is not registered. Register it with AddAgent or AddAgentWorkflow, or point Dex:Routing:DefaultAgent to a registered agent.");
        }
        _logger.LogInformation("Conversation {ConversationId}: agent '{Agent}' selected (requested '{Requested}').", conversationIdText, definition.Name, options?.Instructions);

        var agent = definition.CreateAgent!(_serviceProvider);
        var session = await agent.CreateSessionAsync(cancellationToken);
        ConversationStoreChatHistoryProvider.SetSessionId(session, conversationId);
        if (definition.IsRouter) {
            yield return Stamp(new ChatResponseUpdate(ChatRole.Assistant, [new StepProgressContent("Selecting agent")]));
        }

        // Run failures surface either as ErrorContent updates (workflow-hosted agents) or as exceptions; both end the turn
        // with an ErrorContent the chats service reports. Walk to the innermost exception for the real cause.
        string? failure = null;
        await using var enumerator = agent.RunStreamingAsync(new List<ChatMessage> { message }, session, cancellationToken: cancellationToken).GetAsyncEnumerator(cancellationToken);
        while (true) {
            AgentResponseUpdate update;
            try {
                if (!await enumerator.MoveNextAsync()) {
                    break;
                }
                update = enumerator.Current;
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception exception) {
                while (exception.InnerException is not null) {
                    exception = exception.InnerException;
                }
                failure = exception.Message;
                break;
            }
            if (IsRouterUpdate(update)) {
                // The router's output is its handoff tool call; nothing it says is meant for the user. Keep its token usage only.
                var usage = update.Contents.OfType<UsageContent>().ToList<AIContent>();
                if (usage.Count == 0) {
                    continue;
                }
                yield return Stamp(new ChatResponseUpdate(ChatRole.Assistant, usage));
                continue;
            }
            yield return Stamp(Project(update));
        }
        if (failure is not null) {
            yield return Stamp(new ChatResponseUpdate(ChatRole.Assistant, [new ErrorContent(failure)]));
        }
        // Cancellation just stops the stream rather than raising a failure event — surface it as cancellation.
        cancellationToken.ThrowIfCancellationRequested();

        ChatResponseUpdate Stamp(ChatResponseUpdate update) {
            update.ConversationId = conversationIdText;
            update.MessageId = turnMessageId;
            update.ResponseId = turnResponseId;
            return update;
        }
    }

    private static bool IsRouterUpdate(AgentResponseUpdate update)
        => string.Equals(update.AgentId, AgentsConstants.IntentRouter.AgentId, StringComparison.Ordinal)
        || string.Equals(update.AuthorName, AgentsConstants.IntentRouter.AgentName, StringComparison.Ordinal);

    /// <summary>
    /// Copies the framework's update into a fresh <see cref="ChatResponseUpdate"/>. The framework keeps merging the update
    /// instances it handed out (handoff context sync, response aggregation), so ids are never re-stamped on its own objects.
    /// </summary>
    private static ChatResponseUpdate Project(AgentResponseUpdate update) => new(update.Role, update.Contents.ToList()) {
        AuthorName = update.AuthorName,
        CreatedAt = update.CreatedAt,
        FinishReason = update.FinishReason,
        AdditionalProperties = update.AdditionalProperties,
    };

    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceKey is null ? _serviceProvider.GetService(serviceType) : _serviceProvider.GetKeyedService(serviceType, serviceKey);

    /// <inheritdoc/>
    public void Dispose() {

    }
}
