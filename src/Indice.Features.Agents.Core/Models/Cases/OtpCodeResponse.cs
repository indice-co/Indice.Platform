namespace Indice.Features.Agents.Core.Models.Cases;

/// <summary>
/// Response payload delivered to the OTP verification request port when the user submits the received OTP code.
/// </summary>
/// <param name="Challenge">The pending OTP challenge emitted by the send step.</param>
/// <param name="Code">The user provided OTP code.</param>
public record OtpCodeResponse(
    OtpChallengeOutput Challenge,
    string Code);
