using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Step 2 of the Cases workflow: Requests user to verify ownership of the case by confirming a specific field.
/// Uses a prompt template to generate the verification request with the field name and masked value.
/// </summary>
public sealed class OwnershipVerifierStep : Executor<CaseRetrievalOutput, OwnershipVerificationOutput>
{
    private readonly AgentMessageLocalizer _messageLocalizer;

    /// <summary>Creates a new <see cref="OwnershipVerifierStep"/>.</summary>
    public OwnershipVerifierStep(AgentMessageLocalizer messageLocalizer) : base(nameof(OwnershipVerifierStep)) {
        _messageLocalizer = messageLocalizer ?? throw new ArgumentNullException(nameof(messageLocalizer));
    }

    /// <inheritdoc/>
    public override async ValueTask<OwnershipVerificationOutput> HandleAsync(
        CaseRetrievalOutput caseData,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(caseData);

        var verificationFieldValue = caseData.VerificationValue ?? throw new InvalidOperationException($"Verification field not found in case data.");

        return await ValueTask.FromResult(new OwnershipVerificationOutput(
            CaseRetrievalData: caseData,
            VerificationFieldValue: verificationFieldValue,
            VerificationPrompt: _messageLocalizer.OwnershipVerificationMessagePrompt));
    }
}

/// <summary>
/// Output of the OwnershipVerifier step after requesting user to confirm case ownership.
/// Contains the verification field name and awaits user response.
/// </summary>
/// <param name="CaseRetrievalData">The original case retrieval output forwarded from previous step.</param>
/// <param name="VerificationFieldValue">The value for verification.</param>
/// <param name="VerificationPrompt">The formatted prompt requesting user confirmation.</param>
/// <param name="Attempt">Current ownership validation attempt index.</param>
public record OwnershipVerificationOutput(
    CaseRetrievalOutput CaseRetrievalData,
    string VerificationFieldValue,
    string VerificationPrompt,
    int Attempt = 0);
