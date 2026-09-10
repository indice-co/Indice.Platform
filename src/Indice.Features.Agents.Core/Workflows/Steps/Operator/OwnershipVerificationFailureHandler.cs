using Indice.Features.Agents.Core.Workflows.Steps.Operator;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Terminal failure step for the Operator workflow: reached when ownership verification
/// exhausts its maximum allowed attempts. Returns a <see cref="ValidationFailureOutput"/>
/// that the workflow surfaces as the final output to the caller.
/// </summary>
public sealed class OwnershipVerificationFailureHandler : Executor<UserInputValidationOutput, ValidationFailureOutput>
{
    private readonly int _maxValidationAttempts;
    private readonly AgentMessageLocalizer _messageLocalizer;

    /// <summary>Creates a new <see cref="OwnershipVerificationFailureHandler"/>.</summary>
    public OwnershipVerificationFailureHandler(IOptions<AgentsOptions> options, AgentMessageLocalizer messageLocalizer) : base(nameof(OwnershipVerificationFailureHandler)) {
        _maxValidationAttempts = options.Value.CasesWorkflow.MaxOwnershipValidationAttempts;
        _messageLocalizer = messageLocalizer;
    }

    /// <inheritdoc/>
    public override async ValueTask<ValidationFailureOutput> HandleAsync(
        UserInputValidationOutput validationOutput,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {

        var failureMessage = validationOutput.ErrorMessage
            ?? _messageLocalizer.OwnershipVerificationFailedMaxAttemptsMessage(_maxValidationAttempts);

        return new ValidationFailureOutput(
            ErrorMessage: failureMessage,
            FailureStep: "OwnershipVerification",
            AttemptsExhausted: validationOutput.ValidationAttempt);
    }
}

/// <summary>
/// Output when validation fails after maximum retry attempts have been exhausted.
/// This terminal output is used for workflow routing when retries are exceeded.
/// </summary>
/// <param name="ErrorMessage">The error message to display.</param>
/// <param name="FailureStep">The step where validation failed (e.g., "OwnershipVerification", "OtpValidation").</param>
/// <param name="AttemptsExhausted">Number of attempts made before giving up.</param>
public record ValidationFailureOutput(
    string ErrorMessage,
    string FailureStep,
    int AttemptsExhausted);
