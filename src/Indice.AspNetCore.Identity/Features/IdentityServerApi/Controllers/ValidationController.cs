using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Models;
using Indice.AspNetCore.Identity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Contains operations for validation during registration process.
    /// </summary>
    /// <response code="400">Bad Request</response>
    /// <response code="500">Internal Server Error</response>
    [Route("api/validate")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "identity")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProblemDetailsExceptionFilter]
    internal class ValidationController : ControllerBase
    {
        private readonly IdentityOptions _identityOptions;
        private readonly UserManager<User> _userManager;
        private readonly Dictionary<string, string> _codeToOptionMap = new Dictionary<string, string> {
            { nameof(IdentityErrorDescriber.PasswordTooShort), nameof(PasswordOptions.RequiredLength) },
            { nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric), nameof(PasswordOptions.RequireNonAlphanumeric) },
            { nameof(IdentityErrorDescriber.PasswordRequiresDigit), nameof(PasswordOptions.RequireDigit) },
            { nameof(IdentityErrorDescriber.PasswordRequiresLower), nameof(PasswordOptions.RequireLowercase) },
            { nameof(IdentityErrorDescriber.PasswordRequiresUpper), nameof(PasswordOptions.RequireUppercase) },
            { nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars), nameof(PasswordOptions.RequiredUniqueChars) }
        };

        public ValidationController(IOptionsSnapshot<IdentityOptions> identityOptions, UserManager<User> userManager) {
            _identityOptions = identityOptions?.Value ?? throw new ArgumentNullException(nameof(identityOptions));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        /// <summary>
        /// Validates a user's credentials against the configured <see cref="PasswordOptions"/>.
        /// </summary>
        [HttpGet("credentials")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(CredentialsValidationInfo))]
        public async Task<IActionResult> ValidateUserCredentials([FromQuery]string userName, [FromQuery]string password) {
            var hasUserName = !string.IsNullOrWhiteSpace(userName);
            var hasPassword = !string.IsNullOrWhiteSpace(password);
            if (!hasUserName && !hasPassword) {
                return BadRequest(new ValidationProblemDetails {
                    Title = "You need to provide either a username or a password for validation.",
                    Status = StatusCodes.Status400BadRequest
                });
            }
            var response = new CredentialsValidationInfo();
            if (hasPassword) {
                response.PasswordRules = new List<PasswordRuleInfo>();
                foreach (var validator in _userManager.PasswordValidators) {
                    var pair = GetCodeToOptionMap(validator);
                    if (pair != null) {
                        _codeToOptionMap.Add(pair.Value.Key, pair.Value.Value);
                    }
                    var result = await validator.ValidateAsync(_userManager, new User(userName ?? string.Empty), password);
                    if (!result.Succeeded) {
                        foreach (var error in result.Errors) {
                            response.PasswordRules.Add(new PasswordRuleInfo {
                                Name = error.Code,
                                Description = error.Description,
                                IsValid = false
                            });
                        }
                    }
                }
                foreach (var pair in _codeToOptionMap) {
                    var isContained = response.PasswordRules.SingleOrDefault(rule => rule.Name == pair.Key) != null;
                    if (!isContained) {
                        response.PasswordRules.Add(new PasswordRuleInfo {
                            Name = pair.Key,
                            IsValid = true
                        });
                    }
                }
            }
            if (hasUserName) {
                var foundUser = await _userManager.FindByNameAsync(userName);
                response.UserNameExists = foundUser != null;
            }
            return Ok(response);
        }

        private KeyValuePair<string, string>? GetCodeToOptionMap(IPasswordValidator<User> validator) {
            var validatorType = validator.GetType();
            validatorType = validatorType.IsGenericType ? validatorType.GetGenericTypeDefinition() : validatorType;
            if (validatorType.IsAssignableFrom(typeof(NonCommonPasswordValidator<>))) {
                return new KeyValuePair<string, string>(NonCommonPasswordValidator.ErrorDescriber, nameof(IPasswordBlacklistProvider.Blacklist));
            }
            if (validatorType.IsAssignableFrom(typeof(UserNameAsPasswordValidator))) {
                return new KeyValuePair<string, string>(UserNameAsPasswordValidator.ErrorDescriber, nameof(UserNameAsPasswordValidator.MaxAllowedUserNameSubset));
            }
            if (validatorType.IsAssignableFrom(typeof(PreviousPasswordAwareValidator<,,>))) {
                return new KeyValuePair<string, string>(PreviousPasswordAwareValidator.ErrorDescriber, nameof(PreviousPasswordAwareValidator.PasswordHistoryLimit));
            }
            return default;
        }
    }
}
