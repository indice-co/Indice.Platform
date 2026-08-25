namespace Indice.Features.Agents.Core.Models.Cases;

/// <summary>
/// Output of the OwnershipVerifier step after requesting user to confirm case ownership.
/// Contains the verification field name and awaits user response.
/// </summary>
/// <param name="CaseRetrievalData">The original case retrieval output forwarded from previous step.</param>
/// <param name="VerificationFieldName">The name of the field the user must verify (e.g., "email", "last4digits").</param>
/// <param name="VerificationFieldValue">The value for verification.</param>
/// <param name="VerificationPrompt">The formatted prompt requesting user confirmation.</param>
public record OwnershipVerificationOutput(
    CaseRetrievalOutput CaseRetrievalData,
    string VerificationFieldName,
    string VerificationFieldValue,
    string VerificationPrompt);
