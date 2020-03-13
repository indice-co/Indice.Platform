using System;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Contains various lookups for the system.
    /// </summary>
    /// <response code="400">Bad Request</response>
    /// <response code="500">Internal Server Error</response>
    [Route("api/lookups")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "identity")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: 400, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: 401, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: 403, type: typeof(ProblemDetails))]
    [ProblemDetailsExceptionFilter]
    internal class LookupController : ControllerBase
    {
        private readonly IdentityOptions _identityOptions;

        public LookupController(IOptionsSnapshot<IdentityOptions> identityOptions) {
            _identityOptions = identityOptions?.Value ?? throw new ArgumentNullException(nameof(identityOptions));
        }

        /// <summary>
        /// Gets the password options that are applied when the user creates an account.
        /// </summary>
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
        [HttpGet("password-options")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(PasswordOptions))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public ActionResult<PasswordOptions> GetPasswordOptions() {
            var passwordOptions = _identityOptions.Password;
            // This is not very common to happen since there are default PasswordOptions that are applied.
            if (passwordOptions == null) {
                return NotFound();
            }
            return Ok(passwordOptions);
        }
    }
}
