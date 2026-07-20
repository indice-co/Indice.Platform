using System.Runtime.CompilerServices;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Agents.Core;

/// <summary>Entry point for executing the Dex RAG pipeline against a single user question.</summary>
public interface IDexChatClient : IChatClient
{
}

/// <inheritdoc/>
/// <summary>
/// Creates a new <see cref="AgentsChatClient"/> instance.
/// </summary>
/// <param name="workflow">The workflow instance to execute.</param>
/// <param name="serviceProvider">The service provider for resolving dependencies.</param>
public class AgentsChatClient([FromKeyedServices("Default")] Workflow workflow, IServiceProvider serviceProvider) : IDexChatClient
{

    /// <summary>Human-friendly progress labels keyed by executor id, surfaced as SSE <c>step</c> events.</summary>
    private static readonly IReadOnlyDictionary<string, string> StepLabels = new Dictionary<string, string>(StringComparer.Ordinal) {
        ["IntentClassifier"] = "Classifying intent",
        ["QueryRewriter"] = "Rewriting query",
        ["Retriever"] = "Retrieving relevant context",
        ["Reranker"] = "Ranking results",
        ["AnswerComposer"] = "Composing answer",
        ["PurposeResponder"] = "Answering",
        ["OutOfScopeResponder"] = "Preparing response",
    };

    /// <inheritdoc/>
    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) {
        var stream = GetStreamingResponseAsync(messages, options, cancellationToken);
        var response = await stream.ToChatResponseAsync();
        return response;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        if (messages.Count() != 1) {
            throw new ArgumentException("DexChatClient only supports a single user message per request. No batching allowed.", nameof(messages));
        }
        var message = messages.First();
        var state = new ConversationState(message, options?.ConversationId ?? Guid.NewGuid().ToString());
        await using var run = await InProcessExecution.RunStreamingAsync(workflow, state, sessionId: state.ConversationId, cancellationToken: cancellationToken);

        string? failure = null;
        await foreach (var evt in run.WatchStreamAsync().WithCancellation(cancellationToken)) {
            switch (evt) {
                case AgentResponseUpdateEvent updateEvent:
                    var update = updateEvent.Update.AsChatResponseUpdate();
                    update.ConversationId = state.ConversationId;
                    yield return update;
                    break;
                // One progress event per step start; unmapped executor ids are skipped.
                case ExecutorInvokedEvent invoked when StepLabels.TryGetValue(invoked.ExecutorId, out var label):
                    yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextReasoningContent(label)]) { ConversationId = state.ConversationId };
                    break;
                // A throwing step halts the run; keep the first (richer) message.
                case WorkflowErrorEvent error:
                    failure ??= (error.Data as Exception)?.Message ?? "Workflow failed without exception details.";
                    yield return new ChatResponseUpdate(ChatRole.Assistant, [new ErrorContent(failure)]) { ConversationId = state.ConversationId };
                    break;
                default:
                    //Console.WriteLine(evt);
                    break;
            }
        }
        // Cancellation just stops the stream rather than raising a failure event — surface it as cancellation.
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceKey is null ? serviceProvider.GetService(serviceType) : serviceProvider.GetKeyedService(serviceType, serviceKey);

    /// <inheritdoc/>
    public void Dispose() {

    }
}
