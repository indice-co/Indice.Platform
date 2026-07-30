namespace Indice.Features.Agents.Core.Models.Cases;

/// <summary>
/// Output of OTP code verification, preserving full workflow context for downstream steps.
/// </summary>
/// <param name="OtpResponse">The OTP response envelope containing challenge and case context.</param>
/// <param name="IsValid">Whether the OTP was successfully verified.</param>
/// <param name="Message">User-facing verification message.</param>
public record OtpValidationOutput(
    OtpCodeResponse OtpResponse,
    bool IsValid,
    string Message);
