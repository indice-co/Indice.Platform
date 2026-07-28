namespace Indice.Features.Agents.Core.Models.Cases;

/// <summary>
/// Output of the UserInputValidator step with validation result and retry tracking.
/// </summary>
/// <param name="OwnershipVerificationData">The original ownership verification output.</param>
/// <param name="IsValid">Whether the user's input matches the case data field.</param>
/// <param name="ErrorMessage">Error message if validation failed; null if valid.</param>
/// <param name="ValidationAttempt">Current attempt number (1 or 2; max 2 attempts allowed).</param>
/// <param name="UserInput">The user's input to verify (stored for comparison).</param>
public record UserInputValidationOutput(
    OwnershipVerificationOutput OwnershipVerificationData,
    bool IsValid,
    string? ErrorMessage,
    int ValidationAttempt,
    string UserInput);
