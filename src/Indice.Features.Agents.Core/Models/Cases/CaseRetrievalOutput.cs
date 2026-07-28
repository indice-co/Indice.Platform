using System.Text.Json.Nodes;

namespace Indice.Features.Agents.Core.Models.Cases;

/// <summary>
/// Output of the CaseDataRetriever step containing the retrieved case data and related context.
/// </summary>
/// <param name="CaseData">The complete case data as a JsonNode for flexible schema support.</param>
/// <param name="CaseId">The unique identifier for the case.</param>
/// <param name="UserIdentifier">The user's identifier from claims or context.</param>
/// <param name="PhoneNumber">The phone number from case data for OTP delivery (primary channel).</param>
/// <param name="Email">The email from case data (fallback channel if phone unavailable).</param>
public record CaseRetrievalOutput(
    JsonNode CaseData,
    string CaseId,
    string UserIdentifier,
    string? PhoneNumber,
    string? Email);
