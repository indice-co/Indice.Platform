namespace Indice.Features.Agents.Core.Models.Cases;

/// <summary>
/// Response payload delivered to the Cases workflow when the user replies to the ownership verification request.
/// Produced by the host (chat client) as an external response to the ownership confirmation request port.
/// </summary>
/// <param name="VerificationData">The ownership verification data originally emitted by the OwnershipVerifier step.</param>
/// <param name="UserInput">The raw text the user submitted to confirm ownership of the case.</param>
public record OwnershipConfirmationResponse(
    OwnershipVerificationOutput VerificationData,
    string UserInput);
