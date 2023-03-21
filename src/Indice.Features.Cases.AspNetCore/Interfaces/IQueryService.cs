using System.Security.Claims;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;

namespace Indice.Features.Cases.Interfaces;

/// <summary>IQueryService</summary>
public interface IQueryService
{
    /// <summary>Get saved queries.</summary>
    /// <param name="user">The user that saves the query.</param>
    Task<List<Query>> GetQueries(ClaimsPrincipal user);

    /// <summary>Save a new query.</summary>
    /// <param name="user">The user that saves the query.</param>
    /// <param name="request"></param>
    Task SaveQuery(ClaimsPrincipal user, SaveQueryRequest request);

    /// <summary>Delete a query.</summary>
    /// <param name="user">The user that saves the query.</param>
    /// <param name="queryId">The id of the query.</param>
    Task DeleteQuery(ClaimsPrincipal user, Guid queryId);
}
