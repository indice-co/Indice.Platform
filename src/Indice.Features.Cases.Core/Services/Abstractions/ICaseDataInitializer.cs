using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Models.Responses;

namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>
/// Defines a method for initializing case data for a specified user, case type, and optional contact.
/// </summary>
public interface ICaseDataInitializer
{
    /// <summary>
    /// Initializes a new case for the specified contact and case type asynchronously.
    /// </summary>
    /// <param name="owner">The contact for whom the case is being initialized. Cannot be null.</param>
    /// <param name="caseTypeCode">The code representing the type of case to initialize. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a JsonNode with the initialized case
    /// data, or null if initialization fails.</returns>
    Task<JsonNode?> InitializeAsync(Contact owner, string caseTypeCode);
}