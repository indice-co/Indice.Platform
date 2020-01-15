using System;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using Indice.AspNetCore.Identity.Models;
using Indice.AspNetCore.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Contains operations for managing a user's account.
    /// </summary>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Internal Server Error</response>
    [Route("api/account")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "identity")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Admin)]
    [ProblemDetailsExceptionFilter]
    internal class AccountController : ControllerBase
    {
        private readonly ExtendedUserManager<User> _userManager;
        private readonly ExtendedIdentityDbContext<User, Role> _dbContext;

        public AccountController(ExtendedUserManager<User> userManager, ExtendedIdentityDbContext<User, Role> dbContext) {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public string UserId => User.FindFirstValue(JwtClaimTypes.Subject);

        /// <summary>
        /// Changes the password for a given user, but requires the old password to be present.
        /// </summary>
        /// <param name="request">Contains info about the user password to change.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody]ChangePasswordRequest request) {
            var user = await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == UserId);
            if (user == null) {
                return NotFound();
            }
            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.ToValidationProblemDetails());
            }
            return Ok();
        }
    }
}
