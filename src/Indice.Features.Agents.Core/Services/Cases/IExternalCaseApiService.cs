using System.Text.Json.Nodes;

namespace Indice.Features.Agents.Core.Services.Cases;

/// <summary>
/// Provides abstraction for external case data retrieval via API or MCP.
/// </summary>
public interface IExternalCaseApiService
{
    /// <summary>
    /// Retrieves case data based on user input.
    /// </summary>
    /// <param name="userInput">The user's input to search/query case data.</param>
    /// <param name="userIdentifier">The current user's identifier for authorization/filtering.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Case data as JsonNode for flexible schema support, with at least CaseId, PhoneNumber, and Email fields.</returns>
    ValueTask<JsonNode> RetrieveCaseAsync(string userInput, string userIdentifier, CancellationToken cancellationToken = default);
}
