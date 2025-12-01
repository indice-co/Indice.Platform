using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;

namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>
/// Defines a method for initializing case data for a specified user, case type, and optional contact.
/// </summary>
public interface ICaseDataInitializer
{
    /// <summary>
    /// Initializes a new case asynchronously for the specified user and case type.
    /// </summary>
    /// <param name="user">The user on whose behalf the case is being initialized. Cannot be null.</param>
    /// <param name="caseTypeCode">The code that identifies the type of case to initialize. Cannot be null or empty.</param>
    /// <param name="owner">An optional contact to associate data initialization.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a JsonNode with the initialized case
    /// data, or null if initialization fails.</returns>
    Task<JsonNode?> InitializeAsync(UserActor user, string caseTypeCode, Contact? owner = null);
}