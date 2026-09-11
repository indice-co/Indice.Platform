using System.Runtime.CompilerServices;
using Indice.Features.Agents.Core.Workflows.State;
using Indice.Features.Agents.Core.Workflows.Steps.Operator;
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
    private static IReadOnlyDictionary<string, string> CreateStepLabels(AgentMessageLocalizer localizer) => new Dictionary<string, string>(StringComparer.Ordinal) {
        ["IntentClassifier"] = localizer.StepIntentClassifier,
        ["QueryRewriter"] = localizer.StepQueryRewriter,
        ["Retriever"] = localizer.StepRetriever,
        ["Reranker"] = localizer.StepReranker,
        ["AnswerComposer"] = localizer.StepAnswerComposer,
        ["PurposeResponder"] = localizer.StepPurposeResponder,
        ["OutOfScopeResponder"] = localizer.StepOutOfScopeResponder,
        ["CaseDataRetriever"] = localizer.StepCaseDataRetriever,
        ["OwnershipVerifier"] = localizer.StepOwnershipVerifier,
        ["OtpAgent"] = localizer.StepOtpAgent,
        ["OtpCodeValidator"] = localizer.StepOtpCodeValidator,
        ["OtpRetryChallengeBuilder"] = localizer.StepOtpRetryChallengeBuilder,
        ["CaseDataPresenter"] = localizer.StepCaseDataPresenter,
        ["OwnershipValidatorStep"] = localizer.StepOwnershipValidator,
        ["OtpCodeSendStep"] = localizer.StepOtpCodeSend
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
        // Use options.Instructions as the agent/workflow selector passed from the HTTP layer (ChatRequest.AgentName).
        // Supported selectors: "auto", "knowledge". Unknown or missing values fall back to "knowledge".

        var agenticWorkflowName = options?.Instructions?.Trim().ToLowerInvariant() switch {
            AgentsConstants.AgentNames.Knowledge => AgentsConstants.AgentNames.Knowledge,
            AgentsConstants.AgentNames.Operator => AgentsConstants.AgentNames.Operator,
            _ => AgentsConstants.AgentNames.Knowledge
        };
        var workflow = serviceProvider.GetKeyedService<Workflow>(agenticWorkflowName) ?? serviceProvider.GetRequiredKeyedService<Workflow>(AgentsConstants.AgentNames.Knowledge);
        // Checkpointing: state written via QueueStateUpdateAsync is snapshotted at each superstep into the durable
        // EF-backed store, so it survives across HTTP requests. On a follow-up turn the latest checkpoint of the
        // conversation (sessionId == ConversationId) is restored and the new user message is injected into the resumed run.
        var checkpointManager = serviceProvider.GetRequiredService<CheckpointManager>();
        var latestCheckpoint = options?.ConversationId is not null
            ? await checkpointManager.GetLatestCheckpointAsync(state.ConversationId, cancellationToken)
            : null;
        var isResumed = latestCheckpoint is not null;
        StreamingRun run;
        if (isResumed) {
            run = await InProcessExecution.ResumeStreamingAsync(workflow, latestCheckpoint!, checkpointManager, cancellationToken: cancellationToken);
            //await run.TrySendMessageAsync(state);
        } else {
            run = await InProcessExecution.RunStreamingAsync(workflow, state, checkpointManager, sessionId: state.ConversationId, cancellationToken: cancellationToken);
        }
        await using var _ = run;

        var messageLocalizer = serviceProvider.GetService<AgentMessageLocalizer>() ?? new AgentMessageLocalizer();
        var stepLabels = CreateStepLabels(messageLocalizer);

        RequestInfoEvent? pendingRequest = null;
        string? failure = null;
        string? userReply = isResumed ? message.Text : null;
        await foreach (var evt in run.WatchStreamAsync().WithCancellation(cancellationToken)) {
            switch (evt) {
                case AgentResponseUpdateEvent updateEvent:
                    var update = updateEvent.Update.AsChatResponseUpdate();
                    update.ConversationId = state.ConversationId;
                    yield return update;
                    break;
                // One progress event per step start; unmapped executor ids are skipped. Emitted as ephemeral content, stripped from the composed response.
                case ExecutorInvokedEvent invoked when stepLabels.TryGetValue(invoked.ExecutorId, out var label):
                    yield return new ChatResponseUpdate(ChatRole.Assistant, [new StepProgressContent(label)]) { ConversationId = state.ConversationId };
                    break;
                case RequestInfoEvent requestInfoEvent:
                    pendingRequest = requestInfoEvent;
                    if (requestInfoEvent.Request.PortInfo.PortId == AgentsConstants.WorkflowPorts.OwnershipConfirmation
                        && requestInfoEvent.Request.TryGetDataAs<OwnershipVerificationOutput>(out var verificationData)) {
                        if (userReply is not null) {
                            // Resumed run re-surfaces the pending request — answer it with the user's message.
                            await run.SendResponseAsync(requestInfoEvent.Request.CreateResponse(new OwnershipConfirmationResponse(verificationData!, userReply)));
                            userReply = null;
                        } else {
                            // First pass: surface the verification prompt to the user, persist the checkpoint and halt.
                            yield return new ChatResponseUpdate(ChatRole.Assistant, verificationData!.VerificationPrompt) { ConversationId = state.ConversationId };
                            yield break;
                        }
                    }
                    if (requestInfoEvent.Request.PortInfo.PortId == AgentsConstants.WorkflowPorts.OtpVerification
                        && requestInfoEvent.Request.TryGetDataAs<OtpChallengeOutput>(out var otpChallenge)) {
                        if (userReply is not null) {
                            // Resumed run re-surfaces the OTP request — answer it with the user's code.
                            await run.SendResponseAsync(requestInfoEvent.Request.CreateResponse(new OtpCodeResponse(otpChallenge!, userReply)));
                            userReply = null;
                        } else {
                            // First pass: ask the user for the received OTP and halt.
                            yield return new ChatResponseUpdate(ChatRole.Assistant, otpChallenge!.Prompt) { ConversationId = state.ConversationId };
                            yield break;
                        }
                    }
                    break;
                case SuperStepCompletedEvent:
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
