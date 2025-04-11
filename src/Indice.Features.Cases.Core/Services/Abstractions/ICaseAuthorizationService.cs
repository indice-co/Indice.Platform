using System.Security.Claims;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;

namespace Indice.Features.Cases.Core.Services.Abstractions;

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
    public Task<bool> IsMember(WorkflowActor user, Case @case);

    /// <summary>
    /// When a caseId is requested, it must return the access level of the user
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="caseId">The case id.</param>
    /// <returns></returns>
    public Task<int> MemberAccess(WorkflowActor user, Guid caseId);

    /// <summary>
    /// Return an IQueryable of CasePartials based on the role of the user
    /// </summary>
    /// <param name="user"></param>
    /// <param name="casesQuery"></param>
    /// <returns></returns>
    public Task<IQueryable<CasePartial>> GetCaseMembership(IQueryable<CasePartial> casesQuery, WorkflowActor user);
}