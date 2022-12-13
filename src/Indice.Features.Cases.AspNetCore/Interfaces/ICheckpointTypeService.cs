using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Indice.Features.Cases.Data.Models;

namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// The checkpoint type service for managing the domain models <see cref="DbCheckpoint"/> and <see cref="DbCheckpointType"/>.
    /// </summary>
    internal interface ICheckpointTypeService
    {
        /// <summary>
        /// Get the distinct checkpoint <see cref="DbCheckpointType.Code"/> of the system for the authorized role.
        /// </summary>
        /// <param name="user">The user to filter the case types.</param>
        /// <returns></returns>
        Task<List<string>> GetDistinctCheckpointCodes(ClaimsPrincipal user);
    }
}