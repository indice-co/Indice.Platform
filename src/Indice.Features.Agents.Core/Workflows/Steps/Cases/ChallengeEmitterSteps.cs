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
/// the workflow through <see cref="CasesPhaseRouterStep"/> at <see cref="CustomerDataStage.AwaitOwnershipConfirmation"/>.
/// </summary>
public sealed class OwnershipChallengeEmitterStep : Executor<OwnershipVerificationOutput, OwnershipVerificationOutput>
{
    private readonly IWorkflowStateStore _stateStore;

    /// <summary>Creates a new <see cref="OwnershipChallengeEmitterStep"/>.</summary>
    public OwnershipChallengeEmitterStep(IWorkflowStateStore stateStore) : base(nameof(OwnershipChallengeEmitterStep)) {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    /// <inheritdoc/>
    public override async ValueTask<OwnershipVerificationOutput> HandleAsync(
        OwnershipVerificationOutput verification,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(verification);
        var state = await context.GetCustomerDataStateAsync(_stateStore, cancellationToken) ?? new CustomerDataState();
        state.Stage = CustomerDataStage.AwaitOwnershipConfirmation;
        state.LastStepId = Id;
        state.PendingOwnershipVerification = verification;
        state.PendingOtpChallenge = null;
        await context.SetCustomerDataStateAsync(_stateStore, state, cancellationToken);
        await context.AddEventAsync(
            new AgentResponseUpdateEvent(Id, new AgentResponseUpdate(ChatRole.Assistant, [new TextContent(verification.VerificationPrompt)])),
            cancellationToken);
        return verification;
    }
}

/// <summary>
/// Terminal step that emits the OTP challenge prompt to the user as a normal streamed update, persists the
/// pending challenge and phase, and ends the run. The next user message re-enters the workflow through
/// <see cref="CasesPhaseRouterStep"/> at <see cref="CustomerDataStage.AwaitOtpCode"/>.
/// </summary>
public sealed class OtpChallengeEmitterStep : Executor<OtpChallengeOutput, OtpChallengeOutput>
{
    private readonly IWorkflowStateStore _stateStore;

    /// <summary>Creates a new <see cref="OtpChallengeEmitterStep"/>.</summary>
    public OtpChallengeEmitterStep(IWorkflowStateStore stateStore) : base(nameof(OtpChallengeEmitterStep)) {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    /// <inheritdoc/>
    public override async ValueTask<OtpChallengeOutput> HandleAsync(
        OtpChallengeOutput challenge,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(challenge);
        var state = await context.GetCustomerDataStateAsync(_stateStore, cancellationToken) ?? new CustomerDataState();
        state.Stage = CustomerDataStage.AwaitOtpCode;
        state.LastStepId = Id;
        state.ChannelPath = !string.IsNullOrWhiteSpace(challenge.PhoneNumber) ? "phone" : (!string.IsNullOrWhiteSpace(challenge.Email) ? "email" : null);
        state.PendingOtpChallenge = challenge;
        state.PendingOwnershipVerification = null;
        await context.SetCustomerDataStateAsync(_stateStore, state, cancellationToken);
        await context.AddEventAsync(
            new AgentResponseUpdateEvent(Id, new AgentResponseUpdate(ChatRole.Assistant, [new TextContent(challenge.Prompt)])),
            cancellationToken);
        return challenge;
    }
}
