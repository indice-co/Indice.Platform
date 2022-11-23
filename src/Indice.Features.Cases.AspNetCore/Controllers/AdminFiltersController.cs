using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers
{
    /// <summary>
    /// Manage filters for Back-office users.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(GroupName = CasesApiConstants.Scope)]
    [Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    [Route("[casesApiPrefix]/manage/filters")]
    internal class AdminFiltersController : ControllerBase
    {
        private readonly IFilterService _filterService;

        public AdminFiltersController(IFilterService filterService) {
            _filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
        }

        /// <summary>
        /// Get Filters.
        /// </summary>
        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Filter>))]
        public async Task<IActionResult> GetFilters() {
            var filters = await _filterService.GetFilters(User);
            return Ok(filters);
        }

        /// <summary>
        /// Save a new Filter.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> SaveFilter([FromBody] SaveFilterRequest request) {
            await _filterService.SaveFilter(User, request);
            return NoContent();
        }

        /// <summary>
        /// Deletes a Filter.
        /// </summary>
        /// <param name="filterId">The id of the filter.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("{filterId:guid}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteFilter([FromRoute] Guid filterId) {
            await _filterService.DeleteFilter(User, filterId);
            return NoContent();
        }
    }
}