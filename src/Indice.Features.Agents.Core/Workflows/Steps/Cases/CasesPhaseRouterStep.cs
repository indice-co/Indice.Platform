using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Steps.Cases;

/// <summary>
/// Entry step of the Cases workflow. Loads the persisted <see cref="CasesReplayState"/> for the conversation and
/// routes the incoming user message to the step that matches the current phase:
/// <list type="bullet">
/// <item><see cref="CasesReplayPhase.Start"/> — forwards the <see cref="ConversationState"/> to the case retriever.</item>
/// <item><see cref="CasesReplayPhase.AwaitOwnershipConfirmation"/> — wraps the reply in an <see cref="OwnershipConfirmationResponse"/> for the ownership validator.</item>
/// <item><see cref="CasesReplayPhase.AwaitOtpCode"/> — wraps the reply in an <see cref="OtpCodeResponse"/> for the OTP validator.</item>
/// </list>
/// This removes the need for request ports: every turn is a fresh run that starts at the right phase.
/// </summary>
public sealed class CasesPhaseRouterStep : Executor<ConversationState, object>
{
    private readonly ICasesReplayStateStore _stateStore;

    /// <summary>Creates a new <see cref="CasesPhaseRouterStep"/>.</summary>
    public CasesPhaseRouterStep(ICasesReplayStateStore stateStore) : base(nameof(CasesPhaseRouterStep)) {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    /// <inheritdoc/>
    public override async ValueTask<object> HandleAsync(
        ConversationState conversationState,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(conversationState);
        // Make the conversation state available to downstream steps on every turn.
        await context.SetConversationStateAsync(conversationState, cancellationToken);

        var state = await _stateStore.GetAsync(conversationState.ConversationId, cancellationToken);
        var userInput = conversationState.Message.Text ?? string.Empty;

        switch (state?.Phase) {
            case CasesReplayPhase.AwaitOwnershipConfirmation when state.PendingOwnershipVerification is not null:
                return new OwnershipConfirmationResponse(state.PendingOwnershipVerification, userInput);
            case CasesReplayPhase.AwaitOtpCode when state.PendingOtpChallenge is not null:
                return new OtpCodeResponse(state.PendingOtpChallenge, userInput);
            default:
                // No state, stale terminal state, or corrupted pending payload — start a fresh flow.
                if (state is not null) {
                    await _stateStore.RemoveAsync(conversationState.ConversationId, cancellationToken);
                }
                return conversationState;
        }
    }
}
