using System.Security.Claims;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Models.Requests;
using Indice.Types;

namespace Indice.Features.Cases.Core.Services.Abstractions;

/// <summary>The checkpoint type service for managing the domain models <see cref="Checkpoint"/> and <see cref="CheckpointType"/>.</summary>
public interface ICheckpointTypeService
{
    /// <summary>Get the distinct checkpoint types of the system for the authorized role.
    /// The grouping is done by <see cref="CheckpointType.Code"/> and <see cref="CheckpointType.Title"/>.
    /// </summary>
    /// <param name="user">The user to filter the case types.</param>
    /// <returns></returns>
    Task<List<CheckpointType>> GetDistinctCheckpointTypes(UserActor user);

    /// <summary>Get the checkpoint types list. When <see cref="GetCheckpointTypesListFilter.Distinct"/> is true the result is grouped by code.</summary>
    /// <param name="user">The user to filter the checkpoint types.</param>
    /// <param name="options">List options and filter.</param>
    Task<ResultSet<CheckpointType>> GetList(UserActor user, ListOptions<GetCheckpointTypesListFilter> options);

    /// <summary>Get a checkpoint type by id.</summary>
    /// <param name="id">The checkpoint type id.</param>
    Task<CheckpointType?> GetById(Guid id);

    /// <summary>Create a new checkpoint type.</summary>
    /// <param name="request">The request.</param>
    Task<CheckpointType> Create(CheckpointTypeRequest request);

    /// <summary>Update an existing checkpoint type.</summary>
    /// <param name="id">The checkpoint type id.</param>
    /// <param name="request">The request.</param>
    Task<CheckpointType?> Update(Guid id, CheckpointTypeRequest request);

    /// <summary>Delete a checkpoint type. Throws <see cref="BusinessException"/> when in use.</summary>
    /// <param name="id">The checkpoint type id.</param>
    /// <returns>False if not found.</returns>
    Task<bool> Delete(Guid id);
}