using System.Security.Claims;
using Indice.Features.Cases.Core.Models.Responses;

namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>The checkpoint type service for managing the domain models <see cref="Checkpoint"/> and <see cref="CheckpointType"/>.</summary>
public interface ICheckpointTypeService
{
    /// <summary>Get the distinct checkpoint types of the system for the authorized role.
    /// The grouping is done by <see cref="CheckpointType.Code"/> and <see cref="CheckpointType.Title"/>.
    /// </summary>
    /// <param name="user">The user to filter the case types.</param>
    /// <returns></returns>
    Task<List<CheckpointType>> GetDistinctCheckpointTypes(ClaimsPrincipal user);
}