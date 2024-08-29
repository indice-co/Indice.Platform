using System.Security.Claims;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Requests;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Interfaces;

/// <summary>The Case Type services for managing <see cref="DbCaseMember"/> domain model.</summary>
public interface ICaseMemberService
{
    /// <summary>Get the stake holder linked to the specified case.</summary>
    /// <param name="caseId"></param>
    /// <returns> a list of CaseMember</returns>
    Task<IEnumerable<CaseMember>> Get(Guid caseId);
    /// <summary>
    /// Add a Meber to an existing case 
    /// </summary>
    /// <param name="request"> The information for new object</param>
    /// <returns></returns>
    Task Add(CaseMemberRequest request);

    /// <summary>
    /// Update Member access level for case
    /// </summary>
    /// <param name="request"> The information for new object</param>
    /// <returns></returns>
    Task UpdateAccessLevel(CaseMemberRequest request);

    /// <summary>
    /// Removes CaseMember from the specified case
    /// </summary>
    /// <param name="request">Data for the removal</param>
    /// <returns></returns>
    Task Delete(CaseMemberDeleteRequest request);
}