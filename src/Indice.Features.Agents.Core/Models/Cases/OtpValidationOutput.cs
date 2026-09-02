namespace Indice.Features.Agents.Core.Models.Cases;

/// <summary>
/// Output of OTP code verification, preserving full workflow context for downstream steps.
/// </summary>
/// <param name="OtpResponse">The OTP response envelope containing challenge and case context.</param>
/// <param name="IsValid">Whether the OTP was successfully verified.</param>
/// <param name="Message">User-facing verification message.</param>
/// <param name="ShouldRetry">Whether the workflow should ask for OTP input again.</param>
/// <param name="ShouldResendOtp">Whether a new OTP should be sent before asking again.</param>
/// <param name="FailedAttempts">Number of invalid OTP attempts so far.</param>
/// <param name="MaxFailedAttempts">Maximum invalid OTP attempts allowed.</param>
public record OtpValidationOutput(
    OtpCodeResponse OtpResponse,
    bool IsValid,
    string Message,
    bool ShouldRetry,
    bool ShouldResendOtp,
    int FailedAttempts,
    int MaxFailedAttempts);
