using System.Net.Mime;
using Indice.Features.Cases.Factories;
using Indice.Features.Cases.Models;
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
        private ILookupServiceFactory _lookupServiceFactory { get; }

        /// <inheritdoc/>
        public LookupController(ILookupServiceFactory lookupServiceFactory) {
            _lookupServiceFactory = lookupServiceFactory ?? throw new ArgumentNullException(nameof(lookupServiceFactory));
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
        public async Task<IActionResult> GetLookup([FromRoute] string lookupName, [FromQuery] SearchValues searchValues = null) {
            var lookupService = _lookupServiceFactory.Create(lookupName);
            var lookupItems = await lookupService.Get(searchValues);
            return Ok(lookupItems);
        }
    }
}
