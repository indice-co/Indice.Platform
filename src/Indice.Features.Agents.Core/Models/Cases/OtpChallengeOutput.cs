namespace Indice.Features.Agents.Core.Models.Cases;

/// <summary>
/// Output from the OTP send step. Represents a pending OTP challenge that requires user input.
/// </summary>
/// <param name="ValidationData">The upstream validated ownership data.</param>
/// <param name="Prompt">Prompt shown to the user asking for OTP input.</param>
/// <param name="PhoneNumber">Phone number used for OTP delivery.</param>
/// <param name="Email">Email used for OTP delivery when applicable.</param>
/// <param name="CaseId">Case id associated with this challenge.</param>
/// <param name="FailedAttempts">Number of invalid OTP attempts already made.</param>
/// <param name="MaxFailedAttempts">Maximum invalid OTP attempts allowed before failing.</param>
public record OtpChallengeOutput(
    UserInputValidationOutput ValidationData,
    string Prompt,
    string? PhoneNumber,
    string? Email,
    string CaseId,
    int FailedAttempts = 0,
    int MaxFailedAttempts = 2);
