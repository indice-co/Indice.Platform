using System.Security.Claims;
using Indice.Features.Cases.Core.Models.Responses;

namespace Indice.Features.Cases.Core.Interfaces;

/// <summary>The checkpoint type service for managing the domain models <see cref="DbCheckpoint"/> and <see cref="DbCheckpointType"/>.</summary>
internal interface ICheckpointTypeService
{
    /// <summary>Get the distinct checkpoint types of the system for the authorized role.
    /// The grouping is done by <see cref="CheckpointType.Code"/> and <see cref="CheckpointType.Title"/>.
    /// </summary>
    /// <param name="user">The user to filter the case types.</param>
    /// <returns></returns>
    Task<IEnumerable<CheckpointType>> GetDistinctCheckpointTypes(ClaimsPrincipal user);
}