using System.Text.Json.Nodes;

namespace Indice.Features.Agents.Core.Models.Cases;

/// <summary>
/// Extracts structured fields from a raw JSON case payload and maps them into a <see cref="CaseRetrievalOutput"/>.
/// </summary>
/// <remarks>
/// Register a custom implementation before calling <c>AddCasesWorkflow()</c> to override
/// the default field names used to locate the case id, contact channels, and verification value
/// inside the JSON returned by the MCP case service.
/// </remarks>
public interface ICaseDataExtractor
{
    /// <summary>
    /// Extracts the case identifier from the JSON payload.
    /// </summary>
    /// <param name="caseData">The root <see cref="JsonNode"/> returned by the case retrieval agent.</param>
    /// <returns>The case identifier string.</returns>
    string ExtractCaseId(JsonNode caseData);

    /// <summary>
    /// Extracts the phone number used for OTP delivery from the JSON payload.
    /// Returns <see langword="null"/> when the field is absent or empty.
    /// </summary>
    /// <param name="caseData">The root <see cref="JsonNode"/> returned by the case retrieval agent.</param>
    string? ExtractPhoneNumber(JsonNode caseData);

    /// <summary>
    /// Extracts the e-mail address used as a fallback OTP channel from the JSON payload.
    /// Returns <see langword="null"/> when the field is absent or empty.
    /// </summary>
    /// <param name="caseData">The root <see cref="JsonNode"/> returned by the case retrieval agent.</param>
    string? ExtractEmail(JsonNode caseData);

    /// <summary>
    /// Extracts the value the end-user must supply to verify their identity (e.g. car plate, VIN, contract number).
    /// Returns <see langword="null"/> when the field is absent or not applicable.
    /// </summary>
    /// <param name="caseData">The root <see cref="JsonNode"/> returned by the case retrieval agent.</param>
    string? ExtractVerificationValue(JsonNode caseData);

    /// <summary>
    /// Validates the extracted verification value against the case data to ensure it is present and meets expected criteria.
    /// </summary>
    /// <param name="caseData">The root <see cref="JsonNode"/> returned by the case retrieval agent.</param>
    OperationResult Validate(JsonNode caseData);

}
/// <summary>
/// Represents the result of an operation, including whether it succeeded and an optional error message.
/// </summary>
public record OperationResult(bool Succeeded, string? ErrorMessage)
{
    /// <summary>Creates a successful <see cref="OperationResult"/>.</summary>
    public static OperationResult Success() => new(true, null);
    /// <summary>
    /// Creates a failed <see cref="OperationResult"/> with the specified error message.
    /// </summary>
    /// <param name="error">The error message describing the failure.</param>
    /// <returns>A failed <see cref="OperationResult"/>.</returns>
    public static OperationResult Failure(string error) => new(false, error);
}