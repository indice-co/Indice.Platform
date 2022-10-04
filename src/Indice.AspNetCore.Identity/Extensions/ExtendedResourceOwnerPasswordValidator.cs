using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Indice.AspNetCore.Identity.Data.Models;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Identity
{
    /// <summary><see cref="IResourceOwnerPasswordValidator"/> that integrates with ASP.NET Identity and is specific to mobile clients.</summary>
    public sealed class DeviceResourceOwnerPasswordValidator : ExtendedResourceOwnerPasswordValidator<User>
    {
        /// <summary>Creates a new instance of <see cref="DeviceResourceOwnerPasswordValidator"/>.</summary>
        /// <param name="signInManager">Provides the APIs for user sign in.</param>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        public DeviceResourceOwnerPasswordValidator(
            ExtendedSignInManager<User> signInManager,
            ExtendedUserManager<User> userManager,
            ILogger<ExtendedResourceOwnerPasswordValidator<User>> logger
        ) : base(signInManager, userManager, logger) { }

        /// <inheritdoc />
        public override async Task ValidateAsync(ResourceOwnerPasswordValidationContext context) {
            await base.ValidateAsync(context);
            if (context.Result.IsError) {
                return;
            }
            var client = context.Request.Client;
            if (!client.IsMobile()) {
                Error(context, ResourceOwnerPasswordErrorCodes.NotMobileClient);
                return;
            }
            var deviceId = context.Request.Raw["deviceId"];
            if (string.IsNullOrWhiteSpace(deviceId)) {
                Error(context, ResourceOwnerPasswordErrorCodes.MissingDeviceId);
                return;
            }
            var device = await UserManager.GetDeviceByIdAsync(ValidatingUser, deviceId);
            if (device is null) {
                Error(context, ResourceOwnerPasswordErrorCodes.DeviceNotFound);
                return;
            }
        }
    }

    /// <summary><see cref="IResourceOwnerPasswordValidator"/> that integrates with ASP.NET Identity.</summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    public class ExtendedResourceOwnerPasswordValidator<TUser> : IResourceOwnerPasswordValidator where TUser : User
    {
        private readonly ILogger<ExtendedResourceOwnerPasswordValidator<TUser>> _logger;

        /// <summary>Creates a new instance of <see cref="ExtendedResourceOwnerPasswordValidator{TUser}"/>.</summary>
        /// <param name="signInManager">Provides the APIs for user sign in.</param>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        public ExtendedResourceOwnerPasswordValidator(
            ExtendedSignInManager<TUser> signInManager,
            ExtendedUserManager<TUser> userManager,
            ILogger<ExtendedResourceOwnerPasswordValidator<TUser>> logger
        ) {
            SignInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Provides the APIs for user sign in.</summary>
        protected ExtendedSignInManager<TUser> SignInManager { get; }
        /// <summary>Provides the APIs for managing user in a persistence store.</summary>
        protected ExtendedUserManager<TUser> UserManager { get; }
        /// <summary>The user that is being validated.</summary>
        protected TUser ValidatingUser { get; private set; }
        /// <summary>Error code to description mapping.</summary>
        protected IDictionary<int, string> Errors => new Dictionary<int, string> {
            { ResourceOwnerPasswordErrorCodes.LockedOut, "User is locked out." },
            { ResourceOwnerPasswordErrorCodes.NotAllowed, "User is not allowed." },
            { ResourceOwnerPasswordErrorCodes.InvalidCredentials, "User provided invalid credentials." },
            { ResourceOwnerPasswordErrorCodes.NotFound, "User was not found." },
            { ResourceOwnerPasswordErrorCodes.Blocked, "User is blocked." }
        };

        /// <summary>Validates the resource owner password credential.</summary>
        /// <param name="context">The context.</param>
        public virtual async Task ValidateAsync(ResourceOwnerPasswordValidationContext context) {
            var user = await UserManager.FindByNameAsync(context.UserName);
            if (user is null) {
                Error(context, ResourceOwnerPasswordErrorCodes.NotFound);
                return;
            }
            ValidatingUser = user;
            var result = await SignInManager.CheckPasswordSignInAsync(user, context.Password, lockoutOnFailure: true);
            if (user.Blocked) {
                Error(context, ResourceOwnerPasswordErrorCodes.Blocked);
                return;
            }
            if (result.IsNotAllowed) {
                Error(context, ResourceOwnerPasswordErrorCodes.NotAllowed);
                return;
            }
            if (result.IsLockedOut) {
                Error(context, ResourceOwnerPasswordErrorCodes.LockedOut);
                return;
            }
            if (!result.Succeeded) {
                Error(context, ResourceOwnerPasswordErrorCodes.InvalidCredentials);
                return;
            }
            var subject = await UserManager.GetUserIdAsync(user);
            _logger.LogInformation("Credentials validated for username: '{UserName}'.", context.UserName);
            context.Result = new GrantValidationResult(subject, IdentityModel.OidcConstants.AuthenticationMethods.Password);
        }

        /// <summary></summary>
        /// <param name="context"></param>
        /// <param name="errorCode"></param>
        protected void Error(ResourceOwnerPasswordValidationContext context, int errorCode) {
            _logger.LogInformation("Authentication failed for user: '{UserName}', reason: '{ErrorDescription}'", context.UserName, Errors[errorCode]);
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, errorCode.ToString());
        }
    }

    internal class ResourceOwnerPasswordErrorCodes
    {
        public const int LockedOut = 104;
        public const int NotAllowed = 105;
        public const int InvalidCredentials = 106;
        public const int NotFound = 107;
        public const int Blocked = 108;
        public const int NotMobileClient = 109;
        public const int MissingDeviceId = 110;
        public const int DeviceNotFound = 111;
    }
}
