using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;

namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// IFilterService
    /// </summary>
    public interface IFilterService
    {
        /// <summary>
        /// Get Filters.
        /// </summary>
        /// <param name="user">The user that saves the filter.</param>
        Task<List<Filter>> GetFilters(ClaimsPrincipal user);

        /// <summary>
        /// Save a new Filter.
        /// </summary>
        /// <param name="user">The user that saves the filter.</param>
        /// <param name="request"></param>
        Task SaveFilter(ClaimsPrincipal user, SaveFilterRequest request);

        /// <summary>
        /// Deletes a Filter.
        /// </summary>
        /// <param name="user">The user that saves the filter.</param>
        /// <param name="filterId">The id of the filter.</param>
        Task DeleteFilter(ClaimsPrincipal user, Guid filterId);
    }
}
