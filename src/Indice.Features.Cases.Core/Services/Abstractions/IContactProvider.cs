using System.Security.Claims;
using System.Text.Json.Nodes;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>The customer service that will be integrated from the consumer for customer queries from its own storage/apis.</summary>
public interface IContactProvider
{
    /// <summary>Get a list of customers.</summary>
    /// <param name="user">The current user</param>
    /// <param name="listOptions">The list options for search and pagination</param>
    /// <returns></returns>
    Task<ResultSet<Contact>> GetListAsync(ClaimsPrincipal user, ListOptions<ContactFilter> listOptions);
    /// <summary>Get Customer Data for a specific case type.</summary>
    /// <param name="user">The current user</param>
    /// <param name="reference">The correlation Id to the external system. Can be the customer id or user id etc..</param>
    /// <param name="caseTypeCode">The case type code.</param>
    /// <returns>The contact data in json form or null if not found</returns>
    Task<JsonNode?> GetByReferenceAsync(ClaimsPrincipal user, string reference, string caseTypeCode);
}
