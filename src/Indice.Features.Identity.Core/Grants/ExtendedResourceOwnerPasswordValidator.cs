using System.Text;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.DeviceAuthentication.Configuration;
using Indice.Features.Identity.Core.ImpossibleTravel;
using Indice.Features.Identity.Core.Totp;
using Indice.Services;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.Core.Grants;

/// <summary>An extended implementation of <see cref="IResourceOwnerPasswordValidator"/> where multiple filters can be registered and validated.</summary>
/// <typeparam name="TUser">The type of the user.</typeparam>
/// <remarks>Creates a new instance of <see cref="ExtendedResourceOwnerPasswordValidator{TUser}"/>.</remarks>
/// <param name="filters"></param>
/// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
/// <param name="logger">Represents a type used to perform logging.</param>
/// <exception cref="ArgumentNullException"></exception>
public class ExtendedResourceOwnerPasswordValidator<TUser>(
    IEnumerable<IResourceOwnerPasswordValidationFilter<TUser>> filters,
    ExtendedUserManager<TUser> userManager,
    ILogger<ExtendedResourceOwnerPasswordValidator<TUser>> logger
) : IResourceOwnerPasswordValidator where TUser : User
{
    private readonly ILogger<ExtendedResourceOwnerPasswordValidator<TUser>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IEnumerable<IResourceOwnerPasswordValidationFilter<TUser>> _filters = filters ?? throw new ArgumentNullException(nameof(filters));
    private readonly ExtendedUserManager<TUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));

    private readonly IDictionary<string, string> _errors = new Dictionary<string, string> {
        { ResourceOwnerPasswordErrorCodes.LockedOut, "User is locked out." },
        { ResourceOwnerPasswordErrorCodes.NotAllowed, "User is not allowed." },
        { ResourceOwnerPasswordErrorCodes.InvalidCredentials, "User provided invalid credentials." },
        { ResourceOwnerPasswordErrorCodes.NotFound, "User was not found." },
        { ResourceOwnerPasswordErrorCodes.Blocked, "User is blocked." },
        { ResourceOwnerPasswordErrorCodes.Traveler, "User's login is suspicious." },
        { ResourceOwnerPasswordErrorCodes.NotMobileClient, "Client is not a mobile device." },
        { ResourceOwnerPasswordErrorCodes.MissingDeviceId, "Device id is missing." },
        { ResourceOwnerPasswordErrorCodes.DeviceNotFound, "Device was not found." }
    };

    /// <inheritdoc />
    public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context) {
        var user = await _userManager.FindByNameAsync(context.UserName);
        var extendedContext = new ResourceOwnerPasswordValidationFilterContext<TUser>(context, user);
        if (user is null) {
            LogError(extendedContext);
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, ResourceOwnerPasswordErrorCodes.NotFound);
            return;
        }
        var deviceId = context.Request.Raw[RegistrationRequestParameters.DeviceId];
        var device = await _userManager.GetDeviceByIdAsync(user, deviceId);
        extendedContext.SetDevice(device);
        var isError = false;
        foreach (var filter in _filters.OrderBy(x => x.Order)) {
            await filter.ValidateAsync(extendedContext);
            // If any of the filter results in an error, then we should break out of the loop.
            if (extendedContext.Result.IsError) {
                isError = true;
                LogError(extendedContext);
                context.Result = extendedContext.Result;
                break;
            }
        }
        if (!isError) {
            var subject = await _userManager.GetUserIdAsync(user);
            context.Result = extendedContext.Result;
        }
    }

    private void LogError(ResourceOwnerPasswordValidationContext context) =>
        _logger.LogInformation("Authentication failed for user: '{UserName}', reason: '{ErrorDescription}'",
            context.UserName,
            !string.IsNullOrWhiteSpace(context.Result?.ErrorDescription) && _errors.ContainsKey(context.Result.ErrorDescription)
                ? _errors[context.Result.ErrorDescription]
                : string.Empty
        );
}

/// <summary>Class describing the resource owner password validation context.</summary>
/// <typeparam name="TUser">The type of the user.</typeparam>
public class ResourceOwnerPasswordValidationFilterContext<TUser> : ResourceOwnerPasswordValidationContext where TUser : User
{
    private UserDevice _userDevice;

    internal ResourceOwnerPasswordValidationFilterContext(ResourceOwnerPasswordValidationContext context, TUser user) {
        Password = context.Password;
        Request = context.Request;
        Result = context.Result;
        User = user;
        UserName = context.UserName;
    }

    /// <summary>The user instance.</summary>
    public TUser User { get; }
    /// <summary>The user device.</summary>
    public UserDevice Device => _userDevice;
    internal bool Handled { get; set; }

    /// <summary>Sets the <see cref="Device"/> property.</summary>
    /// <param name="device">The user device.</param>
    internal void SetDevice(UserDevice device) => _userDevice = device;
}

/// <summary>Handles validation of resource owner password credentials.</summary>
/// <typeparam name="TUser">The type of the user.</typeparam>
public interface IResourceOwnerPasswordValidationFilter<TUser> where TUser : User
{
    /// <summary>The order used to run the filter.</summary>
    public int Order { get; }
    /// <summary>Validates the resource owner password credential.</summary>
    /// <param name="context">Class describing the resource owner password validation context.</param>
    Task ValidateAsync(ResourceOwnerPasswordValidationFilterContext<TUser> context);
}

/// <summary><see cref="IResourceOwnerPasswordValidator"/> that integrates with ASP.NET Identity and is specific to mobile clients.</summary>
/// <typeparam name="TUser">The type of the user.</typeparam>
/// <remarks>Creates a new instance of <see cref="DeviceResourceOwnerPasswordValidator{TUser}"/>.</remarks>
/// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
public sealed class DeviceResourceOwnerPasswordValidator<TUser>(ExtendedUserManager<TUser> userManager) : IResourceOwnerPasswordValidationFilter<TUser> where TUser : User
{
    private readonly ExtendedUserManager<TUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));

    /// <inheritdoc />
    public int Order => 1;

    /// <inheritdoc />
    public async Task ValidateAsync(ResourceOwnerPasswordValidationFilterContext<TUser> context) {
        if (context.Device is null) {
            context.Result = context.Result;
            return;
        }
        if (context.Device.RequiresPassword) {
            await _userManager.SetDeviceRequiresPasswordAsync(context.User, context.Device, requiresPassword: false);
        }
        context.Device.LastSignInDate = DateTimeOffset.UtcNow;
        await _userManager.UpdateDeviceAsync(context.User, context.Device);
        context.Result = context.Result;
    }
}

/// <summary><see cref="IResourceOwnerPasswordValidator"/> that integrates with ASP.NET Identity.</summary>
/// <typeparam name="TUser">The type of the user.</typeparam>
public class IdentityResourceOwnerPasswordValidator<TUser> : IResourceOwnerPasswordValidationFilter<TUser> where TUser : User
{
    private readonly ExtendedUserManager<TUser> _userManager;
    private readonly ExtendedSignInManager<TUser> _signInManager;
    private readonly TotpServiceFactory _totpServiceFactory;
    private readonly IdentityMessageDescriber _identityMessageDescriber;
    private readonly ISignInGuard<TUser> _signInGuard;

    /// <summary>Creates a new instance of <see cref="IdentityResourceOwnerPasswordValidator{TUser}"/>.</summary>
    /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
    /// <param name="signInManager">Provides the APIs for user sign in.</param>
    /// <param name="totpServiceFactory">Used to generate, send and verify time based one time passwords.</param>
    /// <param name="identityMessageDescriber">Provides an extensibility point for altering localizing used inside the package.</param>
    /// <param name="signInGuard"></param>
    public IdentityResourceOwnerPasswordValidator(
        ExtendedUserManager<TUser> userManager,
        ExtendedSignInManager<TUser> signInManager,
        TotpServiceFactory totpServiceFactory,
        IdentityMessageDescriber identityMessageDescriber,
        ISignInGuard<TUser> signInGuard) {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _totpServiceFactory = totpServiceFactory ?? throw new ArgumentNullException(nameof(totpServiceFactory));
        _identityMessageDescriber = identityMessageDescriber ?? throw new ArgumentNullException(nameof(identityMessageDescriber));
        _signInGuard = signInGuard ?? throw new ArgumentNullException(nameof(signInGuard));
    }

    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public async Task ValidateAsync(ResourceOwnerPasswordValidationFilterContext<TUser> context) {
        var result = await _signInManager.CheckPasswordSignInAsync(context.User, context.Password, lockoutOnFailure: true);
        if (context.User.Blocked) {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, ResourceOwnerPasswordErrorCodes.Blocked);
            return;
        }
        if (result.IsNotAllowed) {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, ResourceOwnerPasswordErrorCodes.NotAllowed);
            return;
        }
        if (result.IsLockedOut) {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, ResourceOwnerPasswordErrorCodes.LockedOut);
            return;
        }
        var signInGuardResult = await _signInGuard.IsSuspiciousLogin(_signInManager.Context, context.User);
        if (signInGuardResult.Warning == SignInWarning.ImpossibleTravel) {
            if (_signInGuard.ImpossibleTravelDetector.FlowType == ImpossibleTravelFlowType.DenyLogin) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, ResourceOwnerPasswordErrorCodes.Blocked);
                return;
            }
            // In this case, an otp has been sent, or an error has occurred.
            await SendOrValidateImpossibleTravelOtp(context);
            if (context.Handled) {
                return;
            }
        }
        if (!result.Succeeded) {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, ResourceOwnerPasswordErrorCodes.InvalidCredentials);
            return;
        }
        // For Users with 2FA enabled AccessFailedCounter is only reset after successful sign in with the 2nd Factor.
        // We need to override the default logic for mobile login.
        if (context.User.AccessFailedCount > 0) {
            await _userManager.ResetAccessFailedCountAsync(context.User);
        }
        var subject = await _userManager.GetUserIdAsync(context.User);
        context.Result = new GrantValidationResult(subject, OidcConstants.AuthenticationMethods.Password);
    }

    /// <summary></summary>
    /// <param name="context">Class describing the resource owner password validation context.</param>
    public virtual async Task SendOrValidateImpossibleTravelOtp(ResourceOwnerPasswordValidationFilterContext<TUser> context) {
        context.Handled = true;
        var rawRequest = context.Request.Raw;
        var requestId = rawRequest.Get("requestId") ?? Guid.NewGuid().ToString();
        var totpService = _totpServiceFactory.Create<User>();
        if (rawRequest.Get("grant_type") is not { } grantType ||
            rawRequest.Get("username") is not { } username ||
            rawRequest.Get("password") is not { } password ||
            rawRequest.Get("client_id") is not { } clientId ||
            rawRequest.Get("scope") is not { } scope
        ) {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant);
            return;
        }
        var purpose = new StringBuilder()
            .Append($"{grantType}:")
            .Append($"{username}:")
            .Append($"{password}:")
            .Append($"{clientId}:")
            .Append($"{rawRequest.Get("deviceId")}:")
            .Append($"{scope}:")
            .Append($"{requestId}");
        var otp = rawRequest.Get("otp");
        if (string.IsNullOrEmpty(otp)) {
            // User tried to log in, and impossible travel has been detected.
            var providedChannel = rawRequest.Get("channel");
            var channel = TotpDeliveryChannel.Sms;
            if (!string.IsNullOrWhiteSpace(providedChannel)) {
                if (Enum.TryParse<TotpDeliveryChannel>(providedChannel, ignoreCase: true, out var parsedChannel)) {
                    channel = parsedChannel;
                } else {
                    context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Invalid delivery channel.");
                    return;
                }
            }
            await totpService.SendAsync(totp =>
                totp.ToUser(context.User)
                    .WithMessage(_identityMessageDescriber.ImpossibleTravelOtpMessage())
                    .UsingDeliveryChannel(channel)
                    .WithSubject(_identityMessageDescriber.ImpossibleTravelOtpSubject)
                    .WithPurpose(purpose.ToString())
            );
            context.Result = new GrantValidationResult(
                error: TokenRequestErrors.InvalidGrant,
                errorDescription: ResourceOwnerPasswordErrorCodes.Traveler,
                customResponse: new Dictionary<string, object> {{ "requestId", requestId }
            });
            return;
        }
        // User will verify the otp to invalidate the impossible travel.
        if (await totpService.VerifyAsync(context.User, otp, purpose.ToString()) is { Success: false }) {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "OTP verification code could not be validated.");
            return;
        }
        // Return token to user.
        context.Result = new GrantValidationResult(context.User.Id, CustomGrantTypes.Mfa);
    }
}

internal class ResourceOwnerPasswordErrorCodes
{
    // User specific error codes.
    public const string LockedOut = "104";
    public const string NotAllowed = "105";
    public const string InvalidCredentials = "106";
    public const string NotFound = "107";
    public const string Blocked = "108";
    public const string Traveler = "109";
    // Client specific error codes.
    public const string NotMobileClient = "204";
    public const string MissingDeviceId = "205";
    public const string DeviceNotFound = "206";
}
