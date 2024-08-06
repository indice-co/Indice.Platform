using System.Security.Claims;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Requests;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Interfaces;

/// <summary>The Case Type services for managing <see cref="DbStakeHolder"/> domain model.</summary>
public interface IStakeHolderService
{
    /// <summary>Get the stake holder linked to the specified case.</summary>
    /// <param name="caseId"></param>
    /// <returns> a list of stakeholders</returns>
    Task<IEnumerable<StakeHolder>> Get(Guid caseId);
    /// <summary>
    /// Add a stakeholder 
    /// </summary>
    /// <param name="request"> The information for new object</param>
    /// <returns></returns>
    Task Add(StakeHolderRequest request);

    /// <summary>
    /// Update stakeholder access level
    /// </summary>
    /// <param name="request"> The information for new object</param>
    /// <returns></returns>
    Task UpdateAccessLevel(StakeHolderRequest request);

    /// <summary>
    /// Removes stakeholder from the specified case
    /// </summary>
    /// <param name="caseId">The Id of the case</param>
    /// <param name="stakeHolderid">The id of the stakeholder</param>
    /// <param name="type">type of the stakeholder</param>
    /// <returns></returns>
    Task Delete(Guid caseId, string stakeHolderid, byte type);
}