using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Builds the next ownership verification challenge when validation allows a retry.
/// </summary>
public sealed class OwnershipRetryChallengeBuilder : Executor<UserInputValidationOutput, OwnershipVerificationOutput>
{
    /// <summary>Creates a new <see cref="OwnershipRetryChallengeBuilder"/>.</summary>
    public OwnershipRetryChallengeBuilder() : base(nameof(OwnershipRetryChallengeBuilder)) { }

    /// <inheritdoc/>
    public override async ValueTask<OwnershipVerificationOutput> HandleAsync(
        UserInputValidationOutput validationOutput,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {

        ArgumentNullException.ThrowIfNull(validationOutput);

        if (validationOutput.IsValid) {
            throw new InvalidOperationException("Ownership retry challenge requested for valid input.");
        }

        var verificationData = validationOutput.OwnershipVerificationData;
        var retryPrompt = string.IsNullOrWhiteSpace(validationOutput.ErrorMessage)
            ? verificationData.VerificationPrompt
            : $"{validationOutput.ErrorMessage} {verificationData.VerificationPrompt}";

        return await ValueTask.FromResult(verificationData with {
            VerificationPrompt = retryPrompt,
            Attempt = validationOutput.ValidationAttempt
        });
    }
}
