namespace Indice.Features.Agents.Core.Models.Cases;

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
