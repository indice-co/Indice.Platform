using Indice.Features.Agents.Core.Models.Cases;
using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Steps.Cases;

/// <summary>
/// Terminal failure step for the Cases workflow: reached when ownership verification
/// exhausts its maximum allowed attempts. Returns a <see cref="ValidationFailureOutput"/>
/// that the workflow surfaces as the final output to the caller.
/// </summary>
public sealed class OwnershipVerificationFailureHandler : Executor<UserInputValidationOutput, ValidationFailureOutput>
{
    //TODO: MaxValidationAttempts from configuration
    private const int MaxValidationAttempts = 2;

    /// <summary>Creates a new <see cref="OwnershipVerificationFailureHandler"/>.</summary>
    public OwnershipVerificationFailureHandler() : base(nameof(OwnershipVerificationFailureHandler)) { }

    /// <inheritdoc/>
    public override async ValueTask<ValidationFailureOutput> HandleAsync(
        UserInputValidationOutput validationOutput,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {

        var failureMessage = validationOutput.ErrorMessage
            ?? $"Ownership verification failed after {MaxValidationAttempts} attempts.";

        return await ValueTask.FromResult(new ValidationFailureOutput(
            ErrorMessage: failureMessage,
            FailureStep: "OwnershipVerification",
            AttemptsExhausted: validationOutput.ValidationAttempt));
    }
}
