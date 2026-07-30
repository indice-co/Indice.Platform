using System.Runtime.CompilerServices;
using System.Text.Json;
using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.State;
using Indice.Security;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
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
        ["CaseDataRetriever"] = "Retrieve Case Data",
        ["OwnershipVerifier"] = "Verify ownership of Case Data",
        ["OtpAgent"] = "Send OTP",
        ["OtpCodeValidator"] = "Verify OTP code",
        ["CaseDataPresenter"] = "Present case details"
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
        var conversationId = options?.ConversationId ?? Guid.NewGuid().ToString();
        // Use options.Instructions as the agent/workflow selector passed from the HTTP layer (ChatRequest.AgentName).
        // Supported selectors: "auto", "knowledge", "cases". Unknown or missing values fall back to "knowledge".
        var agenticWorkflowName = options?.Instructions?.Trim().ToLowerInvariant() switch {
            AgentsConstants.AgentNames.Auto => AgentsConstants.AgentNames.Auto,
            AgentsConstants.AgentNames.Knowledge => AgentsConstants.AgentNames.Knowledge,
            AgentsConstants.AgentNames.Cases => AgentsConstants.AgentNames.Cases,
            _ => AgentsConstants.AgentNames.Knowledge
        };
        var workflow = serviceProvider.GetKeyedService<Workflow>(agenticWorkflowName)
            ?? serviceProvider.GetRequiredKeyedService<Workflow>(AgentsConstants.AgentNames.Knowledge);

        if (agenticWorkflowName == AgentsConstants.AgentNames.Cases) {
            // The Cases workflow is checkpointed: it halts at the ownership confirmation request port so the
            // user can be asked to verify the case data, and resumes with the user's next message.
            await foreach (var update in RunCasesWorkflowAsync(workflow, message, conversationId, cancellationToken)) {
                yield return update;
            }
        } else {
            var state = new ConversationState(message, conversationId);
            await using var run = await InProcessExecution.RunStreamingAsync(workflow, state, sessionId: conversationId, cancellationToken: cancellationToken);
            await foreach (var update in WatchStreamUpdates(run, conversationId, cancellationToken)) {
                yield return update;
            }
        }

        // Cancellation just stops the stream rather than raising a failure event — surface it as cancellation.
        cancellationToken.ThrowIfCancellationRequested();
    }

    private string PendingStateCacheKey(string conversationId) => $"cases-pending-verification:{conversationId}";

    private async IAsyncEnumerable<ChatResponseUpdate> RunCasesWorkflowAsync(
        Workflow workflow,
        ChatMessage message,
        string conversationId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        var cache = serviceProvider.GetRequiredService<IDistributedCache>();
        var checkpointManager = serviceProvider.GetRequiredService<CheckpointManager>();
        var pendingJson = await cache.GetStringAsync(PendingStateCacheKey(conversationId), cancellationToken);

        StreamingRun run;
        string? userReply = null;
        if (pendingJson is not null) {
            // A previous run halted awaiting the user's verification input — resume it and answer the pending request.
            var pending = JsonSerializer.Deserialize<PendingCasesWorkflowState>(pendingJson)!;
            var checkpointInfo = JsonSerializer.Deserialize<CheckpointInfo>(pending.CheckpointJson)!;
            await cache.RemoveAsync(PendingStateCacheKey(conversationId), cancellationToken);
            userReply = message.Text ?? string.Empty;
            run = await InProcessExecution.ResumeStreamingAsync(workflow, checkpointInfo, checkpointManager, cancellationToken);
        } else {
            var state = new ConversationState(message, conversationId);
            run = await InProcessExecution.RunStreamingAsync(workflow, state, checkpointManager, conversationId, cancellationToken);
        }

        await using var _ = run;
        CheckpointInfo? lastCheckpoint = null;
        var halted = false;
        await foreach (var evt in run.WatchStreamAsync().WithCancellation(cancellationToken)) {
            switch (evt) {
                case RequestInfoEvent requestInfo when requestInfo.Request.PortInfo.PortId == AgentsConstants.OwnershipConfirmationPortId
                    && requestInfo.Request.TryGetDataAs<OwnershipVerificationOutput>(out var verificationData):
                    if (userReply is not null) {
                        // Resumed run re-surfaces the pending request — answer it with the user's message.
                        await run.SendResponseAsync(requestInfo.Request.CreateResponse(new OwnershipConfirmationResponse(verificationData!, userReply)));
                        userReply = null;
                    } else {
                        // First pass: surface the verification prompt to the user, persist the checkpoint and halt.
                        yield return new ChatResponseUpdate(ChatRole.Assistant, verificationData!.VerificationPrompt) { ConversationId = conversationId };
                        halted = true;
                    }
                    break;
                case RequestInfoEvent requestInfo when requestInfo.Request.PortInfo.PortId == AgentsConstants.OtpVerificationPortId
                    && requestInfo.Request.TryGetDataAs<OtpChallengeOutput>(out var otpChallenge):
                    if (userReply is not null) {
                        // Resumed run re-surfaces the OTP request — answer it with the user's code.
                        await run.SendResponseAsync(requestInfo.Request.CreateResponse(new OtpCodeResponse(otpChallenge!, userReply)));
                        userReply = null;
                    } else {
                        // First pass: ask the user for the received OTP and halt.
                        yield return new ChatResponseUpdate(ChatRole.Assistant, otpChallenge!.Prompt) { ConversationId = conversationId };
                        halted = true;
                    }
                    break;
                case SuperStepCompletedEvent superStep when superStep.CompletionInfo?.Checkpoint is not null:
                    lastCheckpoint = superStep.CompletionInfo.Checkpoint;
                    break;
                case AgentResponseUpdateEvent updateEvent:
                    var update = updateEvent.Update.AsChatResponseUpdate();
                    update.ConversationId = conversationId;
                    yield return update;
                    break;
                case ExecutorInvokedEvent invoked when StepLabels.TryGetValue(invoked.ExecutorId, out var label):
                    yield return new ChatResponseUpdate(ChatRole.Assistant, [new StepProgressContent(label)]) { ConversationId = conversationId };
                    break;
                case WorkflowErrorEvent error:
                    var exception = error.Data as Exception;
                    while (exception?.InnerException is not null) {
                        exception = exception.InnerException;
                    }
                    yield return new ChatResponseUpdate(ChatRole.Assistant, [new ErrorContent(exception?.Message ?? "Workflow failed without exception details.")]) { ConversationId = conversationId };
                    break;
            }
            if (halted && lastCheckpoint is not null) {
                // Stop draining the stream: persist the pending state so the next message resumes the run.
                var pendingState = new PendingCasesWorkflowState(JsonSerializer.Serialize(lastCheckpoint));
                await cache.SetStringAsync(
                    PendingStateCacheKey(conversationId),
                    JsonSerializer.Serialize(pendingState),
                    new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromHours(1) },
                    cancellationToken);
                break;
            }
        }
    }

    private async IAsyncEnumerable<ChatResponseUpdate> WatchStreamUpdates(
        StreamingRun run,
        string conversationId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        string? failure = null;
        await foreach (var evt in run.WatchStreamAsync().WithCancellation(cancellationToken)) {
            switch (evt) {
                case AgentResponseUpdateEvent updateEvent:
                    var update = updateEvent.Update.AsChatResponseUpdate();
                    update.ConversationId = conversationId;
                    yield return update;
                    break;
                case ExecutorInvokedEvent invoked when StepLabels.TryGetValue(invoked.ExecutorId, out var label):
                    yield return new ChatResponseUpdate(ChatRole.Assistant, [new StepProgressContent(label)]) { ConversationId = conversationId };
                    break;
                case WorkflowErrorEvent error:
                    var exception = error.Data as Exception;
                    while (exception?.InnerException is not null) {
                        exception = exception.InnerException;
                    }
                    failure ??= exception?.Message ?? "Workflow failed without exception details.";
                    yield return new ChatResponseUpdate(ChatRole.Assistant, [new ErrorContent(failure)]) { ConversationId = conversationId };
                    break;
                default:
                    break;
            }
        }
    }

    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceKey is null ? serviceProvider.GetService(serviceType) : serviceProvider.GetKeyedService(serviceType, serviceKey);

    /// <inheritdoc/>
    public void Dispose() {

    }
}
