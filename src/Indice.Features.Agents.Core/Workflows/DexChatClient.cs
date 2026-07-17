using System.Runtime.CompilerServices;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows.Abstractions;
using Indice.Features.Agents.Core.Workflows.Events;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Agents.Core.Workflows;

/// <inheritdoc/>
public class DexChatClient : IDexChatClient
{
    private readonly Workflow _workflow;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Creates a new <see cref="DexChatClient"/> instance.
    /// </summary>
    /// <param name="workflow">The workflow instance to execute.</param>
    /// <param name="serviceProvider"></param>
    public DexChatClient ([FromKeyedServices("Default")] Workflow workflow, IServiceProvider serviceProvider) {
        _workflow = workflow;
        _serviceProvider = serviceProvider;
    }

    /// <summary>Human-friendly progress labels keyed by executor id, surfaced as SSE <c>step</c> events.</summary>
    private static readonly IReadOnlyDictionary<string, string> StepLabels = new Dictionary<string, string>(StringComparer.Ordinal) {
        ["IntentClassifier"]    = "Classifying intent",
        ["QueryRewriter"]       = "Rewriting query",
        ["Retriever"]           = "Retrieving relevant context",
        ["Reranker"]            = "Ranking results",
        ["AnswerComposer"]      = "Composing answer",
        ["PurposeResponder"]    = "Answering",
        ["OutOfScopeResponder"] = "Preparing response",
    };

    /// <inheritdoc/>
    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) {
        if (messages.Count() != 1) { 
            throw new ArgumentException("DexChatClient only supports a single user message per request. No batching allowed.", nameof(messages));
        }
        var message = messages.First();
        var state = new ConversationState(message, options?.ConversationId ?? Guid.NewGuid().ToString());
        await using var run = await InProcessExecution.RunAsync(_workflow, state, sessionId: state.ConversationId, cancellationToken: cancellationToken);
        RagPipelineOutput? final = null;
        string? failure = null;
        UsageDetails usage = new UsageDetails();
        string? modelUsed = null;
        foreach (var evt in run.NewEvents) {
            switch (evt) {
                // The terminal executors (compose / out-of-scope) are registered via WithOutputFrom, so their
                // returned envelope is yielded as a WorkflowOutputEvent — MAF's dedicated terminal-output channel.
                case WorkflowOutputEvent { Data: RagPipelineOutput env }:
                    final = env;
                    break;
                // Each LLM step reports its own call usage; fold into a single run total.
                case UsageEvent usageEvent:
                    usage.Add(usageEvent.Details);
                    modelUsed = usageEvent.Model;
                    break;
                // A throwing step halts the run; MAF surfaces the original exception here, followed by a
                // WorkflowErrorEvent wrapping it — keep the first (richer) message.
                case ExecutorFailedEvent failed:
                    failure ??= $"{failed.ExecutorId}: {failed.Data?.Message ?? "unknown error"}";
                    break;
                case WorkflowErrorEvent error:
                    failure ??= (error.Data as Exception)?.Message ?? "Workflow failed without exception details.";
                    break;
            }
        }
        // Cancellation never surfaces as a failure event — the run just stops emitting events — so check the
        // caller's token explicitly and report a cancellation rather than a pipeline failure.
        cancellationToken.ThrowIfCancellationRequested();
        if (final is null && failure is null) {
            throw new InvalidOperationException("Workflow completed without emitting a final RagPipelineOutput envelope.");
        }
        

        var assistantText = new TextContent(final?.Answer ?? string.Empty) {
            Annotations = final?.Citations?.Select(c => (AIAnnotation)new CitationAnnotation {
                FileId = c.DocumentId.ToString(),
                Title = final.Sources.First(x => x.Id == c.DocumentId).SourceTitle,
                Url = new Uri(final.Sources.First(x => x.Id == c.DocumentId).SourceUrl),
                Snippet = c.Title
                // Other metadata like page number, confidence, etc.
            }).ToList() ?? []
        };
        var assistantMessage = new ChatMessage(ChatRole.Assistant, [assistantText]) {
            MessageId = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        return new ChatResponse {
            ConversationId = state.ConversationId,
            ResponseId = assistantMessage.MessageId,
            Messages = [assistantMessage],
            ModelId = modelUsed,
            Usage = new() {
                InputTokenCount = usage.InputTokenCount,
                OutputTokenCount = usage.OutputTokenCount,
            },
            AdditionalProperties = new() {
                ["failed"] = failure is not null,
                ["failureReason"] = failure,
                ["sources"] = final?.Sources ?? [],
            }
        };
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation]CancellationToken cancellationToken = default) {
        if (messages.Count() != 1) {
            throw new ArgumentException("DexChatClient only supports a single user message per request. No batching allowed.", nameof(messages));
        }
        var message = messages.First();
        var state = new ConversationState(message, options?.ConversationId ?? Guid.NewGuid().ToString());
        await using var run = await InProcessExecution.RunStreamingAsync(_workflow, state, sessionId: state.ConversationId, cancellationToken: cancellationToken);
        RagPipelineOutput? final = null;
        string? failure = null;
        UsageDetails usage = new UsageDetails();
        string? modelUsed = null;
        var messageId = Guid.NewGuid().ToString();
        await foreach (var evt in run.WatchStreamAsync().WithCancellation(cancellationToken)) {
            switch (evt) {
                // One progress event per step start; unmapped executor ids are skipped.
                case ExecutorInvokedEvent invoked when StepLabels.TryGetValue(invoked.ExecutorId, out var label):
                    yield return new ChatResponseUpdate(ChatRole.Assistant, label) { ConversationId = state.ConversationId, MessageId = messageId, RawRepresentation = "Step" } ;
                    break;
                // Each LLM step reports its own call usage; fold into a single run total.
                case UsageEvent usageEvent:
                    usage.Add(usageEvent.Details);
                    modelUsed = usageEvent.Model;
                    yield return new ChatResponseUpdate(ChatRole.Assistant, [new UsageContent(usage)]) { ConversationId = state.ConversationId, MessageId = messageId, RawRepresentation = "Usage" };
                    break;
                // Answer text deltas emitted by AnswerComposer as the reasoning model streams.
                case AnswerDeltaEvent delta when delta.Delta.Length > 0:
                    yield return new ChatResponseUpdate(ChatRole.Assistant, delta.Delta) { ConversationId = state.ConversationId, MessageId = messageId, RawRepresentation = "Delta" };
                    break;
                // Terminal output from compose / out-of-scope (registered via WithOutputFrom).
                case WorkflowOutputEvent { Data: RagPipelineOutput env }:
                    final = env;
                    break;
                // A throwing step halts the run; keep the first (richer) message.
                case ExecutorFailedEvent failed:
                    failure ??= $"{failed.ExecutorId}: {failed.Data?.Message ?? "unknown error"}";
                    yield return new ChatResponseUpdate(ChatRole.Assistant, failure) { ConversationId = state.ConversationId, MessageId = messageId, RawRepresentation = "Failure" };
                    break;
                case WorkflowErrorEvent error:
                    failure ??= (error.Data as Exception)?.Message ?? "Workflow failed without exception details.";
                    yield return new ChatResponseUpdate(ChatRole.Assistant, failure) { ConversationId = state.ConversationId, MessageId = messageId, RawRepresentation = "Error" };
                    break;
            }
        }
        // Cancellation just stops the stream rather than raising a failure event — surface it as cancellation.
        cancellationToken.ThrowIfCancellationRequested();


        yield return new ChatResponseUpdate(ChatRole.Assistant, final?.Answer ?? "") {
            RawRepresentation = "Final",
            ConversationId = state.ConversationId,
            ResponseId = messageId,
            MessageId = messageId,
            ModelId = modelUsed,
            AdditionalProperties = new() {
                ["failed"] = failure is not null,
                ["failureReason"] = failure,
                ["sources"] = final?.Sources ?? [],
                ["citations"] = final?.Citations ?? [],
            }
        };

    }

    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceKey is null ? _serviceProvider.GetService(serviceType) : _serviceProvider.GetKeyedService(serviceType, serviceKey);

    /// <inheritdoc/>
    public void Dispose() {
        
    }
}
