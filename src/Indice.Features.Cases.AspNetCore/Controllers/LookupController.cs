using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers
{
    /// <summary>
    /// Manage lookups for the case types.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(GroupName = CasesApiConstants.Scope)]
    [Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    [Route("[casesApiPrefix]/manage/lookups")]
    public class LookupController : ControllerBase
    {
        private readonly ILookupService _lookupService;

        /// <inheritdoc/>
        public LookupController(ILookupService lookupService) {
            _lookupService = lookupService ?? throw new ArgumentNullException(nameof(lookupService));
        }

        /// <summary>
        /// Get a lookup by lookupName.
        /// </summary>
        /// <param name="lookupName">The lookup name to retrieve.</param>
        /// <param name="searchValues">Any search values to filter the lookup results.</param>
        /// <returns></returns>
        [HttpGet("{lookupName}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<LookupItem>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> GetLookup([FromRoute] string lookupName, [FromQuery] string searchValues = null) {
            var occupations = await _lookupService.Get(lookupName, searchValues);
            return Ok(occupations);
        }
    }
}
