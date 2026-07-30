namespace Indice.Features.Agents.Core.Models.Cases;

/// <summary>
/// Output from the OTP send step. Represents a pending OTP challenge that requires user input.
/// </summary>
/// <param name="ValidationData">The upstream validated ownership data.</param>
/// <param name="Prompt">Prompt shown to the user asking for OTP input.</param>
/// <param name="PhoneNumber">Phone number used for OTP delivery.</param>
/// <param name="Email">Email used for OTP delivery when applicable.</param>
/// <param name="CaseId">Case id associated with this challenge.</param>
public record OtpChallengeOutput(
    UserInputValidationOutput ValidationData,
    string Prompt,
    string? PhoneNumber,
    string? Email,
    string CaseId);
