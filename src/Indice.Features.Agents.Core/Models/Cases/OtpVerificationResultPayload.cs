using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Features.Agents.Core.Models.Cases;

/// <summary>
/// Raw OTP verification payload returned by the MCP verification tool.
/// </summary>
public sealed record OtpVerificationResultPayload(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("isRateLimited")] bool IsRateLimited,
    [property: JsonPropertyName("isInvalidCode")] bool IsInvalidCode,
    [property: JsonPropertyName("isInvalidFormat")] bool IsInvalidFormat,
    [property: JsonPropertyName("totpLifetime")] int TotpLifetime)
{
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Deserializes a JSON OTP verification payload.
    /// </summary>
    /// <param name="json">The JSON payload to parse.</param>
    /// <returns>A strongly typed OTP verification payload.</returns>
    public static OtpVerificationResultPayload Deserialize(string json) =>
        JsonSerializer.Deserialize<OtpVerificationResultPayload>(json, SerializerOptions)
        ?? throw new InvalidOperationException("Empty JSON payload.");
}
