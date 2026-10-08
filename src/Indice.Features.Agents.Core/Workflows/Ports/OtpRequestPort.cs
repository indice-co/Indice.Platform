using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Ports;

/// <summary>
/// Represents a request port for OTP (One-Time Password) verification in the workflow.
/// </summary>
public static class OtpRequestPort
{
    /// <summary>
    /// Represents a request for an OTP (One-Time Password) with an expiration date.
    /// </summary>
    /// <param name="ChallengeCode">The challenge code of the OTP.</param>
    /// <param name="ExpirationDate">The expiration date of the OTP.</param>
    public record OtpRequest(string ChallengeCode, DateTime ExpirationDate);

    /// <summary>
    /// Represents a response containing an OTP (One-Time Password).
    /// </summary>
    /// <param name="ChallengeCode">The challenge code of the OTP.</param>
    /// <param name="Otp">The OTP (One-Time Password).</param>
    public record OtpResponse(string ChallengeCode, string Otp);

    /// <summary>
    /// Creates a request port for OTP (One-Time Password) verification in the workflow.
    /// </summary>
    /// <param name="id">The identifier for the request port.</param>
    /// <returns>A request port for OTP verification.</returns>
    public static RequestPort<OtpRequest, OtpResponse> Create(string id = nameof(OtpRequest)) => RequestPort.Create<OtpRequest, OtpResponse>(id);
}
