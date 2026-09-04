using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Steps.Cases;

/// <summary>
/// Terminal step that emits the ownership verification prompt to the user as a normal streamed update,
/// persists the pending verification payload and phase, and ends the run. The next user message re-enters
/// the workflow through <see cref="CasesPhaseRouterStep"/> at <see cref="CasesReplayPhase.AwaitOwnershipConfirmation"/>.
/// </summary>
public sealed class OwnershipChallengeEmitterStep : Executor<OwnershipVerificationOutput, OwnershipVerificationOutput>
{
    private readonly ICasesReplayStateStore _stateStore;

    /// <summary>Creates a new <see cref="OwnershipChallengeEmitterStep"/>.</summary>
    public OwnershipChallengeEmitterStep(ICasesReplayStateStore stateStore) : base(nameof(OwnershipChallengeEmitterStep)) {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    /// <inheritdoc/>
    public override async ValueTask<OwnershipVerificationOutput> HandleAsync(
        OwnershipVerificationOutput verification,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(verification);
        var conversationState = await context.GetConversationStateAsync(cancellationToken);
        var state = await _stateStore.GetAsync(conversationState.ConversationId, cancellationToken) ?? new CasesReplayState {
            ConversationId = conversationState.ConversationId
        };
        state.Phase = CasesReplayPhase.AwaitOwnershipConfirmation;
        state.PendingOwnershipVerification = verification;
        state.PendingOtpChallenge = null;
        await _stateStore.SetAsync(state, cancellationToken);
        await context.AddEventAsync(
            new AgentResponseUpdateEvent(Id, new AgentResponseUpdate(ChatRole.Assistant, [new TextContent(verification.VerificationPrompt)])),
            cancellationToken);
        return verification;
    }
}

/// <summary>
/// Terminal step that emits the OTP challenge prompt to the user as a normal streamed update, persists the
/// pending challenge and phase, and ends the run. The next user message re-enters the workflow through
/// <see cref="CasesPhaseRouterStep"/> at <see cref="CasesReplayPhase.AwaitOtpCode"/>.
/// </summary>
public sealed class OtpChallengeEmitterStep : Executor<OtpChallengeOutput, OtpChallengeOutput>
{
    private readonly ICasesReplayStateStore _stateStore;

    /// <summary>Creates a new <see cref="OtpChallengeEmitterStep"/>.</summary>
    public OtpChallengeEmitterStep(ICasesReplayStateStore stateStore) : base(nameof(OtpChallengeEmitterStep)) {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    /// <inheritdoc/>
    public override async ValueTask<OtpChallengeOutput> HandleAsync(
        OtpChallengeOutput challenge,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(challenge);
        var conversationState = await context.GetConversationStateAsync(cancellationToken);
        var state = await _stateStore.GetAsync(conversationState.ConversationId, cancellationToken) ?? new CasesReplayState {
            ConversationId = conversationState.ConversationId
        };
        state.Phase = CasesReplayPhase.AwaitOtpCode;
        state.PendingOtpChallenge = challenge;
        state.PendingOwnershipVerification = null;
        await _stateStore.SetAsync(state, cancellationToken);
        await context.AddEventAsync(
            new AgentResponseUpdateEvent(Id, new AgentResponseUpdate(ChatRole.Assistant, [new TextContent(challenge.Prompt)])),
            cancellationToken);
        return challenge;
    }
}
