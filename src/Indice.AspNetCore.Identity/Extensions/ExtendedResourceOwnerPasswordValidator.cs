using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.AspNetCore.Identity.DeviceAuthentication.Configuration;
using Indice.Configuration;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Identity
{
    /// <summary>An extended implementation of <see cref="IResourceOwnerPasswordValidator"/> where multiple filters can be registered and validated.</summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    public class ExtendedResourceOwnerPasswordValidator<TUser> : IResourceOwnerPasswordValidator where TUser : User
    {
        private readonly ILogger<ExtendedResourceOwnerPasswordValidator<TUser>> _logger;
        private readonly IEnumerable<IResourceOwnerPasswordValidationFilter<TUser>> _filters;
        private readonly ExtendedUserManager<TUser> _userManager;

        private readonly IDictionary<string, string> _errors = new Dictionary<string, string> {
            { ResourceOwnerPasswordErrorCodes.LockedOut, "User is locked out." },
            { ResourceOwnerPasswordErrorCodes.NotAllowed, "User is not allowed." },
            { ResourceOwnerPasswordErrorCodes.InvalidCredentials, "User provided invalid credentials." },
            { ResourceOwnerPasswordErrorCodes.NotFound, "User was not found." },
            { ResourceOwnerPasswordErrorCodes.Blocked, "User is blocked." },
            { ResourceOwnerPasswordErrorCodes.NotMobileClient, "Client is not a mobile device." },
            { ResourceOwnerPasswordErrorCodes.MissingDeviceId, "Device id is missing." },
            { ResourceOwnerPasswordErrorCodes.DeviceNotFound, "Device was not found." }
        };

        /// <summary>Creates a new instance of <see cref="ExtendedResourceOwnerPasswordValidator{TUser}"/>.</summary>
        /// <param name="filters"></param>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ExtendedResourceOwnerPasswordValidator(
            IEnumerable<IResourceOwnerPasswordValidationFilter<TUser>> filters,
            ExtendedUserManager<TUser> userManager,
            ILogger<ExtendedResourceOwnerPasswordValidator<TUser>> logger
        ) {
            _filters = filters ?? throw new ArgumentNullException(nameof(filters));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context) {
            var user = await _userManager.FindByNameAsync(context.UserName);
            var extendedContext = new ResourceOwnerPasswordValidationFilterContext<TUser>(context, user);
            if (user is null) {
                LogError(extendedContext);
                SetError(extendedContext, ResourceOwnerPasswordErrorCodes.NotFound);
                return;
            }
            var isError = false;
            foreach (var filter in _filters) {
                await filter.ValidateAsync(extendedContext);
                // If any of the filter results in an error, then we should break out of the loop.
                if (extendedContext.Result.IsError) {
                    isError = true;
                    LogError(extendedContext);
                    break;
                }
            }
            if (!isError) {
                var subject = await _userManager.GetUserIdAsync(user);
                context.Result = new GrantValidationResult(subject, IdentityModel.OidcConstants.AuthenticationMethods.Password);
            }
        }

        private void SetError(ResourceOwnerPasswordValidationContext context, string errorCode) => context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, errorCode);

        private void LogError(ResourceOwnerPasswordValidationContext context) =>
            _logger.LogInformation("Authentication failed for user: '{UserName}', reason: '{ErrorDescription}'", context.UserName, _errors.ContainsKey(context.Result.ErrorDescription) ? _errors[context.Result.ErrorDescription] : string.Empty);
    }

    /// <summary>Class describing the resource owner password validation context.</summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    public class ResourceOwnerPasswordValidationFilterContext<TUser> : ResourceOwnerPasswordValidationContext where TUser : User
    {
        internal ResourceOwnerPasswordValidationFilterContext(ResourceOwnerPasswordValidationContext context, TUser user) {
            Password = context.Password;
            Request = context.Request;
            Result = context.Result;
            User = user;
            UserName = context.UserName;
        }

        /// <summary>The user instance.</summary>
        public TUser User { get; }
    }

    /// <summary>Handles validation of resource owner password credentials.</summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    public interface IResourceOwnerPasswordValidationFilter<TUser> where TUser : User
    {
        /// <summary>Validates the resource owner password credential.</summary>
        /// <param name="context">Class describing the resource owner password validation context.</param>
        Task ValidateAsync(ResourceOwnerPasswordValidationFilterContext<TUser> context);
    }

    /// <summary><see cref="IResourceOwnerPasswordValidator"/> that integrates with ASP.NET Identity and is specific to mobile clients.</summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    public sealed class DeviceResourceOwnerPasswordValidator<TUser> : IResourceOwnerPasswordValidationFilter<TUser> where TUser : User
    {
        private readonly ExtendedUserManager<TUser> _userManager;

        /// <summary>Creates a new instance of <see cref="DeviceResourceOwnerPasswordValidator{TUser}"/>.</summary>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        public DeviceResourceOwnerPasswordValidator(
            ExtendedUserManager<TUser> userManager
        ) {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        /// <inheritdoc />
        public async Task ValidateAsync(ResourceOwnerPasswordValidationFilterContext<TUser> context) {
            var client = context.Request.Client;
            var deviceId = context.Request.Raw[RegistrationRequestParameters.DeviceId];
            var subject = await _userManager.GetUserIdAsync(context.User);
            if (!client.AllowedGrantTypes.Contains(CustomGrantTypes.DeviceAuthentication) || string.IsNullOrWhiteSpace(deviceId)) {
                context.Result = new GrantValidationResult(subject, IdentityModel.OidcConstants.AuthenticationMethods.Password);
                return;
            }
            var device = await _userManager.GetDeviceByIdAsync(context.User, deviceId);
            if (device is null) {
                SetError(context, ResourceOwnerPasswordErrorCodes.DeviceNotFound);
                return;
            }
            if (device.RequiresPassword) {
                await _userManager.SetDeviceRequiresPasswordAsync(context.User, device, requiresPassword: false);
            }
            context.Result = new GrantValidationResult(subject, IdentityModel.OidcConstants.AuthenticationMethods.Password);
        }

        private void SetError(ResourceOwnerPasswordValidationContext context, string errorCode) => context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, errorCode);
    }

    /// <summary><see cref="IResourceOwnerPasswordValidator"/> that integrates with ASP.NET Identity.</summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    public sealed class IdentityResourceOwnerPasswordValidator<TUser> : IResourceOwnerPasswordValidationFilter<TUser> where TUser : User
    {
        private readonly ExtendedUserManager<TUser> _userManager;
        private readonly ExtendedSignInManager<TUser> _signInManager;

        /// <summary>Creates a new instance of <see cref="IdentityResourceOwnerPasswordValidator{TUser}"/>.</summary>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="signInManager">Provides the APIs for user sign in.</param>
        public IdentityResourceOwnerPasswordValidator(
            ExtendedUserManager<TUser> userManager,
            ExtendedSignInManager<TUser> signInManager
        ) {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        }

        /// <inheritdoc />
        public async Task ValidateAsync(ResourceOwnerPasswordValidationFilterContext<TUser> context) {
            var result = await _signInManager.CheckPasswordSignInAsync(context.User, context.Password, lockoutOnFailure: true);
            if (context.User.Blocked) {
                SetError(context, ResourceOwnerPasswordErrorCodes.Blocked);
                return;
            }
            if (result.IsNotAllowed) {
                SetError(context, ResourceOwnerPasswordErrorCodes.NotAllowed);
                return;
            }
            if (result.IsLockedOut) {
                SetError(context, ResourceOwnerPasswordErrorCodes.LockedOut);
                return;
            }
            if (!result.Succeeded) {
                SetError(context, ResourceOwnerPasswordErrorCodes.InvalidCredentials);
                return;
            }
            var subject = await _userManager.GetUserIdAsync(context.User);
            context.Result = new GrantValidationResult(subject, IdentityModel.OidcConstants.AuthenticationMethods.Password);
        }

        private void SetError(ResourceOwnerPasswordValidationContext context, string errorCode) => context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, errorCode);
    }

    internal class ResourceOwnerPasswordErrorCodes
    {
        public const string LockedOut = "104";
        public const string NotAllowed = "105";
        public const string InvalidCredentials = "106";
        public const string NotFound = "107";
        public const string Blocked = "108";
        public const string NotMobileClient = "204";
        public const string MissingDeviceId = "205";
        public const string DeviceNotFound = "206";
    }
}
