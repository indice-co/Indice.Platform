using System.Security.Claims;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Models.Requests;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Interfaces;

/// <summary>The checkpoint type service for managing the domain models <see cref="DbCheckpoint"/> and <see cref="DbCheckpointType"/>.</summary>
internal interface ICheckpointTypeService
{
    /// <summary>
    /// Creates a new checkpoint type
    /// </summary>
    /// <param name="createCheckPointTypeRequest"></param>
    /// <returns></returns>
    Task<CheckpointType> CreateCheckpointType(CheckpointTypeRequest createCheckPointTypeRequest);

    /// <summary>Get the distinct checkpoint types of the system for the authorized role.
    /// The grouping is done by <see cref="CheckpointType.Code"/> and <see cref="CheckpointType.Title"/>.
    /// </summary>
    /// <param name="user">The user to filter the case types.</param>
    /// <returns></returns>
    Task<IEnumerable<CheckpointType>> GetDistinctCheckpointTypes(ClaimsPrincipal user);

    /// <summary>
    /// Gets the distinct checkpoint types of the casetype specified
    /// </summary>
    /// <param name="user"></param>
    /// <param name="caseTypeId"></param>
    /// <returns></returns>
    Task<ResultSet<CheckpointType>> GetCaseTypeCheckpointTypes(ClaimsPrincipal user, Guid caseTypeId);

    /// <summary>
    /// Gets a checkpoint type by id
    /// </summary>
    /// <param name="checkpointTypeId"></param>
    /// <returns></returns>
    Task<GetCheckpointTypeResponse> GetCheckpointTypeById(Guid checkpointTypeId);

    /// <summary>
    /// Edits a checkpoint type
    /// </summary>
    /// <param name="editCheckpointTypeRequest"></param>
    /// <returns></returns>
    Task<CheckpointType> EditCheckpointType(EditCheckpointTypeRequest editCheckpointTypeRequest);
}