using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Indice.AspNetCore.Identity.Entities;
using Microsoft.Extensions.Logging;
using static IdentityModel.OidcConstants;

namespace Indice.AspNetCore.Identity
{
    /// <summary>
    /// <see cref="IResourceOwnerPasswordValidator"/> that integrates with ASP.NET Identity.
    /// </summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    public class ExtendedResourceOwnerPasswordValidator<TUser> : IResourceOwnerPasswordValidator where TUser : User
    {
        private readonly ExtendedSignInManager<TUser> _signInManager;
        private readonly ExtendedUserManager<TUser> _userManager;
        private readonly ILogger<ExtendedResourceOwnerPasswordValidator<TUser>> _logger;
        private readonly IDictionary<int, string> _errors = new Dictionary<int, string> {
            { ResourceOwnerPasswordErrorCodes.PasswordExpired, "User's password is expired." },
            { ResourceOwnerPasswordErrorCodes.PasswordConfirmation, "User's email requires confirmation." },
            { ResourceOwnerPasswordErrorCodes.PhoneNumberConfirmation, "User's phone number requires confirmation." },
            { ResourceOwnerPasswordErrorCodes.LockedOut, "User is locked out." },
            { ResourceOwnerPasswordErrorCodes.NotAllowed, "User is not allowed." },
            { ResourceOwnerPasswordErrorCodes.InvalidCredentials, "User provided invalid credentials." },
            { ResourceOwnerPasswordErrorCodes.NotFound, "User was not found." }
        };

        /// <summary>
        /// Creates a new instance of <see cref="ExtendedResourceOwnerPasswordValidator{TUser}"/>.
        /// </summary>
        /// <param name="signInManager">Provides the APIs for user sign in.</param>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        public ExtendedResourceOwnerPasswordValidator(
            ExtendedSignInManager<TUser> signInManager,
            ExtendedUserManager<TUser> userManager,
            ILogger<ExtendedResourceOwnerPasswordValidator<TUser>> logger
        ) {
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validates the resource owner password credential.
        /// </summary>
        /// <param name="context">The context.</param>
        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context) {
            var user = await _userManager.FindByNameAsync(context.UserName);
            if (user == null) {
                Error(context, ResourceOwnerPasswordErrorCodes.NotFound);
            }
            var result = await _signInManager.CheckPasswordSignInAsync(user, context.Password, lockoutOnFailure: true);
            if (result.IsNotAllowed) {
                Error(context, ResourceOwnerPasswordErrorCodes.NotAllowed);
                return;
            }
            if (result.IsLockedOut) {
                Error(context, ResourceOwnerPasswordErrorCodes.LockedOut);
                return;
            }
            if (user.PasswordExpired) {
                Error(context, ResourceOwnerPasswordErrorCodes.PasswordExpired);
                return;
            }
            if (_signInManager.RequirePostSignInConfirmedEmail && !user.EmailConfirmed) {
                Error(context, ResourceOwnerPasswordErrorCodes.PasswordConfirmation);
                return;
            }
            if (_signInManager.RequirePostSignInConfirmedPhoneNumber && !user.PhoneNumberConfirmed) {
                Error(context, ResourceOwnerPasswordErrorCodes.PhoneNumberConfirmation);
                return;
            }
            if (!result.Succeeded) {
                Error(context, ResourceOwnerPasswordErrorCodes.InvalidCredentials);
                return;
            }
            var subject = await _userManager.GetUserIdAsync(user);
            _logger.LogInformation("Credentials validated for username: '{UserName}'.", context.UserName);
            context.Result = new GrantValidationResult(subject, AuthenticationMethods.Password);
        }

        private void Error(ResourceOwnerPasswordValidationContext context, int errorCode) {
            _logger.LogInformation("Authentication failed for user: '{UserName}', reason: '{ErrorDescription}'", context.UserName, _errors[errorCode]);
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, errorCode.ToString());
        }
    }

    internal class ResourceOwnerPasswordErrorCodes
    {
        public const int PasswordExpired = 101;
        public const int PasswordConfirmation = 102;
        public const int PhoneNumberConfirmation = 103;
        public const int LockedOut = 104;
        public const int NotAllowed = 105;
        public const int InvalidCredentials = 106;
        public const int NotFound = 107;
    }
}
