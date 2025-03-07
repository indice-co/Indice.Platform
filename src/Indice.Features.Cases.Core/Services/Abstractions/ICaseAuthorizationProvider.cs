using System.Security.Claims;
using Indice.Features.Cases.Core.Models.Responses;

namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>
/// This is the interface for the class
/// that gets all authz services and 
/// combines them.
/// 
/// it has to be of different type that 
/// ICaseAuthorizationService so as not to 
/// be a member of the list of ICaseAuthorizationServices
/// </summary>
public interface ICaseAuthorizationProvider
{
    /// <summary>Validates that a user is authorized against a list of <see cref="ICaseAuthorizationService"/> for a <see cref="Case"/>.</summary>
    /// <param name="user">The user.</param>
    /// <param name="case">The case.</param>
    /// <returns></returns>
    public Task<bool> IsMember(ClaimsPrincipal user, Case @case);

    /// <summary>
    /// Return an IQueryable of CasePartials based on the role of the user
    /// </summary>
    /// <param name="user"></param>
    /// <param name="queryable"></param>
    /// <returns></returns>
    public Task<IQueryable<CasePartial>> GetCaseMembership(IQueryable<CasePartial> queryable, ClaimsPrincipal user);
}
