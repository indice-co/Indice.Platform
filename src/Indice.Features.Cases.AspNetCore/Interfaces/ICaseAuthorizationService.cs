using System.Security.Claims;
using Indice.Features.Cases.Models.Responses;

namespace Indice.Features.Cases.Interfaces;

/// <summary>
/// When a case list is requested by the BO, it is filtered based on rules
/// that are set in services implementing this interface
/// </summary>
public interface ICaseAuthorizationService
{
    /// <summary>
    /// When a caseId is requested, it must return true in order to reach the 
    /// caller
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="case">The case.</param>
    /// <returns></returns>
    public Task<bool> IsValid(ClaimsPrincipal user, Case @case);

    /// <summary>
    /// Return an IQueryable of CasePartials based on the role of the user
    /// </summary>
    /// <param name="user"></param>
    /// <param name="queryable"></param>
    /// <returns></returns>
    public Task<IQueryable<CasePartial>> GetCaseMembership(IQueryable<CasePartial> queryable, ClaimsPrincipal user);
}