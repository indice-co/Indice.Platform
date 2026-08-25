using Indice.Features.Agents.Core.Models.Cases;
using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Steps.Cases;

/// <summary>
/// Builds the next ownership verification challenge when validation allows a retry.
/// </summary>
public sealed class OwnershipRetryChallengeBuilder : Executor<UserInputValidationOutput, OwnershipVerificationOutput>
{
    private const int MaxValidationAttempts = 2;

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

        if (validationOutput.ValidationAttempt >= MaxValidationAttempts) {
            throw new InvalidOperationException("Ownership retry challenge requested after maximum validation attempts were reached.");
        }

        var verificationData = validationOutput.OwnershipVerificationData;
        var attemptsLeft = Math.Max(MaxValidationAttempts - validationOutput.ValidationAttempt, 0);
        var retryPrompt = $"{validationOutput.ErrorMessage} You have {attemptsLeft} attempt(s) left. {verificationData.VerificationPrompt}";

        return await ValueTask.FromResult(verificationData with {
            VerificationPrompt = retryPrompt
        });
    }
}
