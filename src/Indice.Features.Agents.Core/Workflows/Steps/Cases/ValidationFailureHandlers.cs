using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Cases;

/// <summary>
/// Terminal failure step for the Cases workflow: reached when ownership verification
/// exhausts its maximum allowed attempts. Returns a <see cref="ValidationFailureOutput"/>
/// that the workflow surfaces as the final output to the caller.
/// </summary>
public sealed class OwnershipVerificationFailureHandler : Executor<UserInputValidationOutput, ValidationFailureOutput>
{
    private readonly int _maxValidationAttempts;
    private readonly AgentMessageLocalizer _messageLocalizer;
    private readonly ICasesReplayStateStore _stateStore;

    /// <summary>Creates a new <see cref="OwnershipVerificationFailureHandler"/>.</summary>
    public OwnershipVerificationFailureHandler(IOptions<AgentsOptions> options, AgentMessageLocalizer messageLocalizer, ICasesReplayStateStore stateStore) : base(nameof(OwnershipVerificationFailureHandler)) {
        _maxValidationAttempts = options.Value.CasesWorkflow.MaxOwnershipValidationAttempts;
        _messageLocalizer = messageLocalizer;
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    /// <inheritdoc/>
    public override async ValueTask<ValidationFailureOutput> HandleAsync(
        UserInputValidationOutput validationOutput,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {

        // Terminal step — the conversation's Cases state machine is finished.
        var conversationState = await context.GetConversationStateAsync(cancellationToken);
        await _stateStore.RemoveAsync(conversationState.ConversationId, cancellationToken);

        var failureMessage = validationOutput.ErrorMessage
            ?? _messageLocalizer.OwnershipVerificationFailedMaxAttemptsMessage(_maxValidationAttempts);

        return new ValidationFailureOutput(
            ErrorMessage: failureMessage,
            FailureStep: "OwnershipVerification",
            AttemptsExhausted: validationOutput.ValidationAttempt);
    }
}
