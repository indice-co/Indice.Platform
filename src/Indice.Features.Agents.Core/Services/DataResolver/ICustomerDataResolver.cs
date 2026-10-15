using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Indice.Features.Agents.Core.Workflows.State;

namespace Indice.Features.Agents.Core.Services.DataResolver;

/// <summary>
/// Extracts structured fields from a raw JSON case payload and maps them into a <see cref="CustomerData"/>.
/// </summary>
/// <remarks>
/// Register a custom implementation as a keyed service, using the case type code as the key, to override
/// the default field names used to locate the case id, contact channels, and verification value
/// inside the JSON returned by the MCP case service. Case types without a registered resolver
/// fall back to <c>DefaultCustomerDataResolver</c>.
/// </remarks>
public interface ICustomerDataResolver
{
    /// <summary>
    /// Resolves the raw JSON case payload into a structured <see cref="CustomerData"/> object.
    ///</summary>
    /// <param name="caseData">The root <see cref="JsonNode"/> returned by the case retrieval agent.</param>
    /// <returns>The resolved <see cref="CustomerData"/>.</returns>
    CustomerData Resolve(JsonNode caseData);

    /// <summary>
    /// Validates the extracted data value to ensure required fields are present and meet expected criteria.
    /// </summary>
    /// <param name="data">The resolved <see cref="CustomerData"/> to validate.</param>
    OperationResult Validate(CustomerData data);

    /// <summary>
    /// Resolves and validates the raw JSON case payload in a single call.
    /// </summary>
    /// <param name="caseData">The root <see cref="JsonNode"/> returned by the case retrieval agent.</param>
    /// <param name="data">The resolved <see cref="CustomerData"/> when successful; otherwise <see langword="null"/>.</param>
    /// <param name="error">The error message when unsuccessful; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the data was resolved and is valid; otherwise <see langword="false"/>.</returns>
    bool TryResolve(JsonNode caseData, [NotNullWhen(true)] out CustomerData? data, out string? error) {
        ArgumentNullException.ThrowIfNull(caseData);
        data = null;
        CustomerData resolved;
        try {
            resolved = Resolve(caseData);
        } catch (InvalidOperationException ex) {
            error = ex.Message;
            return false;
        }
        var result = Validate(resolved);
        if (!result.Succeeded) {
            error = result.ErrorMessage;
            return false;
        }
        data = resolved;
        error = null;
        return true;
    }
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