using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using Indice.AspNetCore.Identity.Models;
using Indice.AspNetCore.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        private readonly UserEmailVerificationOptions _userEmailVerificationOptions;
        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "Account";

        public AccountController(ExtendedUserManager<User> userManager, ExtendedIdentityDbContext<User, Role> dbContext, UserEmailVerificationOptions userEmailVerificationOptions) {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _userEmailVerificationOptions = userEmailVerificationOptions;
        }

        public string UserId => User.FindFirstValue(JwtClaimTypes.Subject);

        /// <summary>
        /// Changes the username for the current user.
        /// </summary>
        /// <param name="request">Models a request for changing the username.</param>
        [HttpPut("change-username")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> ChangeUserName([FromBody]ChangeUserNameRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var result = await _userManager.SetUserNameAsync(user, request.UserName);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.ToValidationProblemDetails());
            }
            return Ok();
        }

        /// <summary>
        /// Changes the password for the current user, but requires the old password to be present.
        /// </summary>
        /// <param name="request">Contains info about the user password to change.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPut("change-password")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> ChangePassword([FromBody]ChangePasswordRequest request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) {
                return NotFound();
            }
            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.ToValidationProblemDetails());
            }
            return Ok();
        }

        /// <summary>
        /// Confirms the email address of a given user.
        /// </summary>
        /// <param name="userId">The Id of the user.</param>
        /// <param name="code">The confirmation token of the user.</param>
        /// <response code="200">OK</response>
        [AllowAnonymous]
        [HttpGet("confirm-email")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> ConfirmEmail([FromQuery]string userId, [FromQuery]string code) {
            if (userId == null) {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> {
                    { nameof(userId), new string[] { $"Parameter {nameof(userId)} is missing." } }
                }));
            }
            if (userId == null) {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> {
                    { nameof(code), new string[] { $"Parameter {nameof(code)} is missing." } }
                }));
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> {
                    { "user", new string[] { $"Could not find user with id {userId}." } }
                }));
            }
            if (await _userManager.IsEmailConfirmedAsync(user)) {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> {
                    { "user", new string[] { $"User's email is already confirmed." } }
                }));
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.ToValidationProblemDetails());
            }
            // TODO: Maybe add some more checks about the ReturnUrl option.
            if (!string.IsNullOrEmpty(_userEmailVerificationOptions?.ReturnUrl)) {
                return Redirect(_userEmailVerificationOptions.ReturnUrl);
            }
            return Ok();
        }
    }
}
