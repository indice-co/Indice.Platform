using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Steps.Cases;

/// <summary>
/// Entry step of the Cases workflow. Loads the persisted <see cref="CustomerDataState"/> for the conversation and
/// routes the incoming user message to the step that matches the current phase:
/// <list type="bullet">
/// <item><see cref="CustomerDataStage.Start"/> — forwards the <see cref="ConversationState"/> to the case retriever.</item>
/// <item><see cref="CustomerDataStage.AwaitOwnershipConfirmation"/> — wraps the reply in an <see cref="OwnershipConfirmationResponse"/> for the ownership validator.</item>
/// <item><see cref="CustomerDataStage.AwaitOtpCode"/> — wraps the reply in an <see cref="OtpCodeResponse"/> for the OTP validator.</item>
/// </list>
/// This removes the need for request ports: every turn is a fresh run that starts at the right phase.
/// </summary>
public sealed class CasesPhaseRouterStep : Executor<ConversationState, object>
{
    private readonly IWorkflowStateStore _stateStore;

    /// <summary>Creates a new <see cref="CasesPhaseRouterStep"/>.</summary>
    public CasesPhaseRouterStep(IWorkflowStateStore stateStore) : base(nameof(CasesPhaseRouterStep)) {
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

        var state = await context.GetCustomerDataStateAsync(_stateStore, cancellationToken);
        var userInput = conversationState.Message.Text ?? string.Empty;

        switch (state?.Stage) {
            case CustomerDataStage.AwaitOwnershipConfirmation when state.PendingOwnershipVerification is not null:
                return new OwnershipConfirmationResponse(state.PendingOwnershipVerification, userInput);
            case CustomerDataStage.AwaitOtpCode when state.PendingOtpChallenge is not null:
                return new OtpCodeResponse(state.PendingOtpChallenge, userInput);
            default:
                // No state, stale terminal state, or corrupted pending payload — start a fresh flow.
                if (state is not null) {
                    await context.SetCustomerDataStateAsync(_stateStore, null, cancellationToken);
                }
                return conversationState;
        }
    }
}
