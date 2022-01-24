using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Api.Filters;
using Indice.AspNetCore.Identity.Api.Security;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.AspNetCore.Identity.Api.Controllers
{
    /// <summary>
    /// Contains various lookups.
    /// </summary>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [ApiController]
    [ApiExplorerSettings(GroupName = "identity")]
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme)]
    [ProblemDetailsExceptionFilter]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
    [Route("api/lookups/external-providers")]
    internal class LookupsController : ControllerBase
    {
        private readonly IAuthenticationSchemeProvider _schemeProvider;

        public LookupsController(IAuthenticationSchemeProvider schemeProvider) {
            _schemeProvider = schemeProvider ?? throw new ArgumentNullException(nameof(schemeProvider));
        }

        /// <summary>
        /// Gets the list of available external providers.
        /// </summary>
        /// <response code="200">OK</response>
        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(List<ExternalProvider>))]
        public async Task<IActionResult> GetExternalProviders() {
            var providers = (await _schemeProvider.GetAllSchemesAsync())
                .Where(x => x.DisplayName != null)
                .Select(x => new ExternalProvider {
                    DisplayName = x.DisplayName ?? x.Name,
                    AuthenticationScheme = x.Name
                })
                .ToList();
            return Ok(providers);
        }
    }
}
