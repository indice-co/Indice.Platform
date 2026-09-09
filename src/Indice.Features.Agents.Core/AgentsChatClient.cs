using System.Runtime.CompilerServices;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Models;
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
        ["CasesPhaseRouterStep"] = "Resuming case verification",
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

        // All workflows (knowledge, auto, cases) share the same streaming loop. The Cases workflow handles
        // multi-turn state internally: its phase router reads the persisted CasesReplayState and starts each
        // run at the correct step, so no request ports or host-side resume logic are required.
        var state = new ConversationState(message, conversationId);
        await using var run = await InProcessExecution.RunStreamingAsync(workflow, state, sessionId: conversationId, cancellationToken: cancellationToken);
        await foreach (var update in WatchStreamUpdates(run, conversationId, cancellationToken)) {
            yield return update;
        }

        // Cancellation just stops the stream rather than raising a failure event — surface it as cancellation.
        cancellationToken.ThrowIfCancellationRequested();
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

    private static string? NormalizeWorkflowName(string? value) {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized is AgentsConstants.AgentNames.Knowledge or AgentsConstants.AgentNames.Cases ? normalized : null;
    }

    /// <inheritdoc/>
    public void Dispose() {

    }
}
