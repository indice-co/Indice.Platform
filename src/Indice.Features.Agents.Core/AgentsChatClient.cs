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
/// <param name="serviceProvider">The service provider for resolving dependencies.</param>
public class AgentsChatClient(IServiceProvider serviceProvider) : IDexChatClient
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
        var state = new ConversationState(message, options?.ConversationId ?? Guid.NewGuid().ToString());
        // The workflow name is either specified in the options or defaults to "Default" which is out of the box registered workflow of type knowledgebase retrieval.
        // Any other workflow can be registered and used by specifying its name in the options.
        // TODO: We should validate the workflow name and provide a meaningful error if it's not found.
        //       And we should extract this logic to a factory interface that can be used
        //       to create the workflow and validate the name.

        var agenticWorkflowName = options?.Instructions?.ToLowerInvariant() switch { 
            AgentsConstants.AgentNames.Auto => AgentsConstants.AgentNames.Auto,
            AgentsConstants.AgentNames.Knowledge => AgentsConstants.AgentNames.Knowledge,
            _ => AgentsConstants.AgentNames.Knowledge
        };
        var workflow = serviceProvider.GetKeyedService<Workflow>(agenticWorkflowName) ?? serviceProvider.GetRequiredKeyedService<Workflow>(AgentsConstants.AgentNames.Knowledge);

        await using var run = await InProcessExecution.RunStreamingAsync(workflow, state, sessionId: state.ConversationId, cancellationToken: cancellationToken);

        string? failure = null;
        await foreach (var evt in run.WatchStreamAsync().WithCancellation(cancellationToken)) {
            switch (evt) {
                case AgentResponseUpdateEvent updateEvent:
                    var update = updateEvent.Update.AsChatResponseUpdate();
                    update.ConversationId = state.ConversationId;
                    yield return update;
                    break;
                // One progress event per step start; unmapped executor ids are skipped. Emitted as ephemeral content, stripped from the composed response.
                case ExecutorInvokedEvent invoked when StepLabels.TryGetValue(invoked.ExecutorId, out var label):
                    yield return new ChatResponseUpdate(ChatRole.Assistant, [new StepProgressContent(label)]) { ConversationId = state.ConversationId };
                    break;
                // A throwing step halts the run; keep the first (richer) message. The runtime wraps executor
                // exceptions ("Error invoking handler for ..."), so walk to the innermost exception for the real cause.
                case WorkflowErrorEvent error:
                    var exception = error.Data as Exception;
                    while (exception?.InnerException is not null) {
                        exception = exception.InnerException;
                    }
                    failure ??= exception?.Message ?? "Workflow failed without exception details.";
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
