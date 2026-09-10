using System.Runtime.CompilerServices;
using System.Text.Json;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.State;
using Indice.Security;
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
        ["CaseDataRetriever"] = "Retrieve Case Data",
        ["OwnershipVerifier"] = "Verify ownership of Case Data",
        ["OtpAgent"] = "Send OTP",
        ["OtpCodeValidator"] = "Verify OTP code",
        ["OtpRetryChallengeBuilder"] = "Prepare OTP retry",
        ["CaseDataPresenter"] = "Present case details",
        ["OwnershipValidatorStep"] = "Validating ownership confirmation",
        ["OtpCodeSendStep"] = "Send OTP"
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
        var conversationIdGuid = Guid.TryParse(conversationId, out var parsedConversationId) ? parsedConversationId : (Guid?)null;
        var optionsSnapshot = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AgentsOptions>>().Value;
        var workflowRouter = serviceProvider.GetRequiredService<IWorkflowRouter>();
        var conversationStore = serviceProvider.GetRequiredService<IConversationStore>();
        var checkpointManager = serviceProvider.GetRequiredService<CheckpointManager>();
        // Use options.Instructions as the workflow selector passed from the HTTP layer (ChatRequest.AgentName).
        // Explicit selectors (knowledge/cases) win. auto performs LLM routing when enabled.
        var selector = options?.Instructions?.Trim().ToLowerInvariant();
        var explicitWorkflowChoice = selector == AgentsConstants.AgentNames.Auto ? NormalizeWorkflowName(message.Text) : null;
        WorkflowRoutingDecision? routingDecision = null;
        string? persistedWorkflow = null;
        if (selector == AgentsConstants.AgentNames.Auto && conversationIdGuid is Guid cid) {
            persistedWorkflow = await conversationStore.GetSelectedWorkflowAsync(cid, cancellationToken).ConfigureAwait(false);
        }
        var agenticWorkflowName = selector switch {
            AgentsConstants.AgentNames.Knowledge => AgentsConstants.AgentNames.Knowledge,
            AgentsConstants.AgentNames.Cases => AgentsConstants.AgentNames.Cases,
            AgentsConstants.AgentNames.Auto when explicitWorkflowChoice is not null => explicitWorkflowChoice,
            AgentsConstants.AgentNames.Auto when persistedWorkflow is AgentsConstants.AgentNames.Knowledge or AgentsConstants.AgentNames.Cases => persistedWorkflow,
            AgentsConstants.AgentNames.Auto => optionsSnapshot.Routing.Enabled
                ? (routingDecision = await workflowRouter.RouteAsync(message, cancellationToken).ConfigureAwait(false)) is not null
                    && !routingDecision.IsAmbiguous
                    && routingDecision.Confidence >= optionsSnapshot.Routing.ConfidenceThreshold
                    ? routingDecision.WorkflowName
                    : AgentsConstants.AgentNames.Auto
                : AgentsConstants.AgentNames.Knowledge,
            _ => AgentsConstants.AgentNames.Knowledge
        };

        if (agenticWorkflowName == AgentsConstants.AgentNames.Auto) {
            var decision = routingDecision ?? await workflowRouter.RouteAsync(message, cancellationToken).ConfigureAwait(false);
            var clarification = decision.ClarificationPrompt ?? "I can help with either knowledge questions or case-specific requests. Which one do you want? Reply with 'knowledge' or 'cases'.";
            var choices = decision.ClarificationOptions?.Count > 0 ? decision.ClarificationOptions.ToList() : [AgentsConstants.AgentNames.Knowledge, AgentsConstants.AgentNames.Cases];
            yield return new ChatResponseUpdate(ChatRole.Assistant, [
                new TextContent(clarification),
                DataContentExtensions.JsonPart(new MultipleChoice { Options = choices }, AgentsConstants.MediaTypes.MultipleChoice)
            ]) { ConversationId = conversationId };
            yield break;
        }

        if (selector == AgentsConstants.AgentNames.Auto && conversationIdGuid is Guid conversationGuid &&
            agenticWorkflowName is AgentsConstants.AgentNames.Knowledge or AgentsConstants.AgentNames.Cases &&
            !string.Equals(persistedWorkflow, agenticWorkflowName, StringComparison.Ordinal)) {
            await conversationStore.SetSelectedWorkflowAsync(conversationGuid, agenticWorkflowName, cancellationToken).ConfigureAwait(false);
        }

        var workflow = serviceProvider.GetKeyedService<Workflow>(agenticWorkflowName)
            ?? serviceProvider.GetRequiredKeyedService<Workflow>(AgentsConstants.AgentNames.Knowledge);

        // Cases workflow uses native checkpoint pause/resume through request ports.
        if (agenticWorkflowName == AgentsConstants.AgentNames.Cases && conversationIdGuid is Guid casesConversationId) {
            var pending = await conversationStore.GetPendingCasesWorkflowAsync(casesConversationId, cancellationToken).ConfigureAwait(false);
            if (pending is not null && !string.Equals(pending.WorkflowName, AgentsConstants.AgentNames.Cases, StringComparison.OrdinalIgnoreCase)) {
                pending = null;
            }
            if (pending is not null && DateTimeOffset.UtcNow - pending.CreatedAt > TimeSpan.FromHours(2)) {
                pending = null;
                await conversationStore.SetPendingCasesWorkflowAsync(casesConversationId, null, cancellationToken).ConfigureAwait(false);
            }
            if (pending is not null &&
                (string.IsNullOrWhiteSpace(pending.PortId) ||
                 string.IsNullOrWhiteSpace(pending.RequestId) ||
                 string.IsNullOrWhiteSpace(pending.CheckpointJson) ||
                 string.IsNullOrWhiteSpace(pending.RequestPayloadJson))) {
                pending = null;
                await conversationStore.SetPendingCasesWorkflowAsync(casesConversationId, null, cancellationToken).ConfigureAwait(false);
                yield return new ChatResponseUpdate(ChatRole.Assistant, [new ErrorContent("The pending verification session is invalid or expired. Please retry your request.")]) { ConversationId = conversationId };
                yield break;
            }
            StreamingRun? run = null;
            string? startError = null;
            try {
                run = await StartOrResumeCasesRunAsync(workflow, checkpointManager, conversationId, pending, message.Text ?? string.Empty, cancellationToken);
            } catch {
                await conversationStore.SetPendingCasesWorkflowAsync(casesConversationId, null, cancellationToken).ConfigureAwait(false);
                startError = pending is null
                    ? "Unable to execute the cases workflow. Please retry your request."
                    : "Unable to resume the pending verification step. It may have expired; please retry your request.";
            }
            if (startError is not null) {
                yield return new ChatResponseUpdate(ChatRole.Assistant, [new ErrorContent(startError)]) { ConversationId = conversationId };
                yield break;
            }

            await using (run!) {

                RequestInfoEvent? pendingRequest = null;
                CheckpointInfo? checkpoint = null;

                await foreach (var update in WatchStreamUpdates(run, conversationId, blockOnPendingRequest: false, onRequestInfo: evt => pendingRequest = evt, onCheckpoint: cp => checkpoint = cp, cancellationToken: cancellationToken)) {
                    yield return update;
                }

                var status = await run.GetStatusAsync(cancellationToken);
                if (status == RunStatus.PendingRequests && pendingRequest?.Request is { } request && checkpoint is not null) {
                    var nextPending = new PendingCasesWorkflowState(
                        CheckpointJson: JsonSerializer.Serialize(checkpoint),
                        PortId: request.PortInfo.PortId,
                        RequestId: request.RequestId,
                        RequestPayloadJson: JsonSerializer.Serialize(request.Data),
                        WorkflowName: AgentsConstants.AgentNames.Cases,
                        CreatedAt: DateTimeOffset.UtcNow);
                    await conversationStore.SetPendingCasesWorkflowAsync(casesConversationId, nextPending, cancellationToken).ConfigureAwait(false);
                } else {
                    await conversationStore.SetPendingCasesWorkflowAsync(casesConversationId, null, cancellationToken).ConfigureAwait(false);
                }
            }
        } else {
            var state = new ConversationState(message, conversationId);
            await using var run = await InProcessExecution.RunStreamingAsync(workflow, state, sessionId: conversationId, cancellationToken: cancellationToken);
            await foreach (var update in WatchStreamUpdates(run, conversationId, blockOnPendingRequest: true, cancellationToken: cancellationToken)) {
                yield return update;
            }
        }

        // Cancellation just stops the stream rather than raising a failure event — surface it as cancellation.
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async IAsyncEnumerable<ChatResponseUpdate> WatchStreamUpdates(
        StreamingRun run,
        string conversationId,
        bool blockOnPendingRequest,
        Action<RequestInfoEvent>? onRequestInfo = null,
        Action<CheckpointInfo?>? onCheckpoint = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        string? failure = null;
        await foreach (var evt in run.WatchStreamAsync(blockOnPendingRequest, cancellationToken).WithCancellation(cancellationToken)) {
            switch (evt) {
                case AgentResponseUpdateEvent updateEvent:
                    var update = updateEvent.Update.AsChatResponseUpdate();
                    update.ConversationId = conversationId;
                    yield return update;
                    break;
                case ExecutorInvokedEvent invoked when StepLabels.TryGetValue(invoked.ExecutorId, out var label):
                    yield return new ChatResponseUpdate(ChatRole.Assistant, [new StepProgressContent(label)]) { ConversationId = conversationId };
                    break;
                case RequestInfoEvent requestInfoEvent:
                    onRequestInfo?.Invoke(requestInfoEvent);
                    break;
                case SuperStepCompletedEvent superStepCompleted:
                    onCheckpoint?.Invoke(superStepCompleted.CompletionInfo?.Checkpoint);
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

    private static string? NormalizeWorkflowName(string? value) {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized is AgentsConstants.AgentNames.Knowledge or AgentsConstants.AgentNames.Cases ? normalized : null;
    }

    private static async ValueTask<StreamingRun> StartOrResumeCasesRunAsync(
        Workflow workflow,
        CheckpointManager checkpointManager,
        string conversationId,
        PendingCasesWorkflowState? pending,
        string userInput,
        CancellationToken cancellationToken) {

        if (pending is null) {
            var state = new ConversationState(new ChatMessage(ChatRole.User, userInput), conversationId);
            return await InProcessExecution.RunStreamingAsync(workflow, state, checkpointManager, sessionId: state.ConversationId, cancellationToken: cancellationToken);
        }

        var checkpoint = JsonSerializer.Deserialize<CheckpointInfo>(pending.CheckpointJson)
            ?? throw new InvalidOperationException("Pending checkpoint payload is invalid.");

        var run = await InProcessExecution.ResumeStreamingAsync(workflow, checkpoint, checkpointManager, cancellationToken);
        var response = CreateResponseForPendingRequest(pending, userInput);
        await run.SendResponseAsync(response);
        return run;
    }

    private static ExternalResponse CreateResponseForPendingRequest(PendingCasesWorkflowState pending, string userInput) {
        return pending.PortId switch {
            var port when string.Equals(port, AgentsConstants.PortIds.OwnershipConfirmation, StringComparison.Ordinal) => CreateOwnershipResponse(pending, userInput),
            var port when string.Equals(port, AgentsConstants.PortIds.OtpVerification, StringComparison.Ordinal) => CreateOtpResponse(pending, userInput),
            _ => throw new InvalidOperationException($"Unknown pending request port '{pending.PortId}'.")
        };
    }

    private static ExternalResponse CreateOwnershipResponse(PendingCasesWorkflowState pending, string userInput) {
        var requestData = JsonSerializer.Deserialize<OwnershipVerificationOutput>(pending.RequestPayloadJson)
            ?? throw new InvalidOperationException("Ownership verification request payload is invalid.");
        var port = RequestPort.Create<OwnershipVerificationOutput, OwnershipConfirmationResponse>(AgentsConstants.PortIds.OwnershipConfirmation);
        var request = ExternalRequest.Create(port, requestData, pending.RequestId);
        var response = new OwnershipConfirmationResponse(requestData, userInput, requestData.Attempt);
        return request.CreateResponse(response);
    }

    private static ExternalResponse CreateOtpResponse(PendingCasesWorkflowState pending, string userInput) {
        var requestData = JsonSerializer.Deserialize<OtpChallengeOutput>(pending.RequestPayloadJson)
            ?? throw new InvalidOperationException("OTP challenge request payload is invalid.");
        var port = RequestPort.Create<OtpChallengeOutput, OtpCodeResponse>(AgentsConstants.PortIds.OtpVerification);
        var request = ExternalRequest.Create(port, requestData, pending.RequestId);
        var response = new OtpCodeResponse(requestData, userInput);
        return request.CreateResponse(response);
    }

    /// <inheritdoc/>
    public void Dispose() {

    }
}
