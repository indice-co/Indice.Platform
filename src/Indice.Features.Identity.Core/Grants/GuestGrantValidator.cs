using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Indice.Configuration;
using Indice.Globalization;
using Indice.Security;
using Indice.Services;
#if NET9_0_OR_GREATER
using Duende.IdentityModel;
#else
using IdentityModel;
#endif
#if NET9_0_OR_GREATER
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
#else
using IdentityServer4.Models;
using IdentityServer4.Validation;
#endif

namespace Indice.Features.Identity.Core.Grants;

/// <summary>A custom <see cref="IExtensionGrantValidator"/> that issues short-lived tokens for anonymous (guest) users.</summary>
/// <param name="pushNotificationService">The push notification service used to register the guest device for push notifications.</param>
/// <param name="logger">The logger instance.</param>
/// <remarks>
/// The effective subject is echoed back through the token response custom field <c>sub</c>, so callers do not need to parse the access token.
/// Issued identities are discriminated by the <c>idp</c> claim with value <c>guest</c>.
/// Subclass and override <see cref="GetClaimsAsync"/> to validate additional request data and enrich the issued claims,
/// then register the subclass through <c>AddGuestGrantValidator&lt;TValidator&gt;()</c>.
/// </remarks>
public class GuestGrantValidator(IPushNotificationService pushNotificationService, ILogger<GuestGrantValidator> logger) : IExtensionGrantValidator
{
    /// <summary>The identity provider value used to discriminate guest identities.</summary>
    public const string IdentityProviderName = "guest";

    /// <inheritdoc />
    public string GrantType => CustomGrantTypes.Guest;

    /// <summary>The push notification service used to register the guest device for push notifications.</summary>
    public IPushNotificationService PushNotificationService { get; } = pushNotificationService;

    /// <summary>The logger instance.</summary>
    public ILogger<GuestGrantValidator> Logger { get; } = logger;

    /// <inheritdoc />
    public virtual async Task ValidateAsync(ExtensionGrantValidationContext context) {
        string subject = Guid.NewGuid().ToString();
        IEnumerable<Claim> claims;
        try {
            claims = [.. GetProfileClaims(context), .. await GetClaimsAsync(context, subject) ?? []];
            await TryRegisterToPushNotificationsAsync(context, subject);
        } catch (InvalidOperationException exception) {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, exception.Message);
            return;
        }
        context.Result = new GrantValidationResult(
            subject: subject,
            authenticationMethod: CustomGrantTypes.Guest,
            claims: claims,
            identityProvider: IdentityProviderName,
            customResponse: new Dictionary<string, object> {
                [JwtClaimTypes.Subject] = subject
            }
        );
    }

    /// <summary>Attempts to register the guest device for push notifications if all required parameters are present.</summary>
    /// <remarks>The method will try to register the device only if all required parameters are present. 
    /// In case of invalid arguments, an <see cref="InvalidOperationException"/> is thrown. 
    /// In case the registration fails, the exception is swallowed and logged.</remarks>
    /// <exception cref="InvalidOperationException">Thrown when the device fields are not in the correct format.</exception>
    protected async Task TryRegisterToPushNotificationsAsync(ExtensionGrantValidationContext context, string subject) {
        var raw = context.Request.Raw;
        var deviceId = raw.Get(GuestGrantRequestParameterNames.DeviceId)?.Trim();
        var devicePlatform = raw.Get(GuestGrantRequestParameterNames.DevicePlatform)?.Trim();
        var pnsHandle = raw.Get(GuestGrantRequestParameterNames.PnsHandle)?.Trim();
        if (!string.IsNullOrWhiteSpace(deviceId) &&
            !string.IsNullOrWhiteSpace(devicePlatform) &&
            !string.IsNullOrWhiteSpace(pnsHandle)) {
            Guard.TooLong(deviceId, TextSizePresets.S64, GuestGrantRequestParameterNames.DeviceId);
            Guard.TooLong(pnsHandle, TextSizePresets.M256, GuestGrantRequestParameterNames.PnsHandle);
            Guard.TooLong(devicePlatform, TextSizePresets.S32, GuestGrantRequestParameterNames.DevicePlatform);
            var platform = Guard.DevicePlatform(devicePlatform, GuestGrantRequestParameterNames.DevicePlatform);
            try {
                await PushNotificationService.Register(deviceId, pnsHandle, platform, subject);
            } catch {
                // log and swallow the exception, since push notification registration is not critical for the guest token issuance.
                // we log to inform that probably the push notification service is not configured
                // or the device registration failed for some reason.
                Logger.LogError("Failed to register push notification user handle.");
            }
        
        }
    }

    /// <summary>
    /// Parses the optional profile parameters supported out of the box (<c>given_name</c>, <c>family_name</c> and <c>email</c>)
    /// from the raw grant request and maps each one, when present, to the corresponding claim.
    /// </summary>
    /// <param name="context">The extension grant validation context.</param>
    protected static IEnumerable<Claim> GetProfileClaims(ExtensionGrantValidationContext context) {
        var raw = context.Request.Raw;
        var givenName = raw.Get(GuestGrantRequestParameterNames.GivenName)?.Trim();
        if (!string.IsNullOrWhiteSpace(givenName)) {
            Guard.TooLong(givenName, TextSizePresets.M128, GuestGrantRequestParameterNames.GivenName);
            yield return new Claim(JwtClaimTypes.GivenName, givenName);
        }
        var familyName = raw.Get(GuestGrantRequestParameterNames.FamilyName)?.Trim();
        if (!string.IsNullOrWhiteSpace(familyName)) {
            Guard.TooLong(familyName, TextSizePresets.M128, GuestGrantRequestParameterNames.FamilyName);
            yield return new Claim(JwtClaimTypes.FamilyName, familyName);
        }
        var email = raw.Get(GuestGrantRequestParameterNames.Email)?.Trim();
        if (!string.IsNullOrWhiteSpace(email)) {
            Guard.TooLong(email, TextSizePresets.M128, GuestGrantRequestParameterNames.Email);
            Guard.Email(email, GuestGrantRequestParameterNames.Email);
            yield return new Claim(JwtClaimTypes.Email, email);

            /* generate non conflicting username */
            var issuerUri = context.Request.Options.IssuerUri;
            if (issuerUri != null) {
                var issuerDomain = new Uri(issuerUri).Authority;
                var name = email.ToLowerInvariant().Replace('.', '_').Replace('@', '_') + "#EXT#@" + issuerDomain;
                yield return new Claim(JwtClaimTypes.Name, name);
            }
        }
        var phoneNumber = raw.Get(GuestGrantRequestParameterNames.PhoneNumber)?.Trim();
        if (!string.IsNullOrWhiteSpace(phoneNumber)) {
            Guard.TooLong(phoneNumber, TextSizePresets.S16, GuestGrantRequestParameterNames.PhoneNumber);
            var parsedPhone = Guard.Phone(phoneNumber, GuestGrantRequestParameterNames.PhoneNumber);
            yield return new Claim(JwtClaimTypes.PhoneNumber, parsedPhone.ToString());
        }
        var device_id = raw.Get(GuestGrantRequestParameterNames.DeviceId)?.Trim();
        if (!string.IsNullOrWhiteSpace(device_id)) {
            Guard.TooLong(device_id, TextSizePresets.S64, GuestGrantRequestParameterNames.DeviceId);
            yield return new Claim(BasicClaimTypes.DeviceId, device_id);
        }
    }

    /// <summary>Builds the additional claims to include in the issued guest token. The base implementation returns an empty set.</summary>
    /// <remarks>Override to validate extra request data (available through <c>context.Request.Raw</c>) and/or include additional claims. Throw to reject the request with an <c>invalid_grant</c> error.</remarks>
    /// <param name="context">The extension grant validation context.</param>
    /// <param name="subject">The effective guest subject identifier.</param>
    protected virtual Task<IEnumerable<Claim>> GetClaimsAsync(ExtensionGrantValidationContext context, string subject) =>
        Task.FromResult(Enumerable.Empty<Claim>());

    /// <summary>A helper class for guarding against invalid input parameters.</summary>
    protected static class Guard
    {
        /// <summary>Throws an exception if <paramref name="argument"/> is null or empty.</summary>
        /// <param name="argument">The string argument to validate as non-null and non-empty.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
        /// <exception cref="InvalidOperationException"><paramref name="argument"/> is null or empty.</exception>
        public static void NullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null) {
            if (string.IsNullOrEmpty(argument)) {
                throw new InvalidOperationException($"Parameter '{paramName}' cannot be null or empty.");
            }
        }

        /// <summary>Throws an exception if the given string value exceeds the specified maximum length.</summary>
        /// <exception cref="InvalidOperationException"><paramref name="argument"/> is too long.</exception>
        public static void TooLong(string? argument, int maxLength, [CallerArgumentExpression(nameof(argument))] string? paramName = null) {
            if (argument?.Length > maxLength) {
                throw new InvalidOperationException($"Parameter '{paramName}' is too long. Max allowed characters {maxLength}");
            }
        }

        /// <summary>Throws an exception if the given string value is not a valid email address.</summary>
        /// <exception cref="InvalidOperationException"><paramref name="argument"/> is not a valid email address.</exception>
        public static void Email(string argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null) {
            if (argument is null) {
                throw new InvalidOperationException($"Parameter '{paramName}' cannot be null.");
            }
            if (!RegexUtilities.IsValidEmail(argument)) {
                throw new InvalidOperationException($"Invalid email format: {argument}");
            }
        }

        /// <summary>Throws an exception if the given string value is not a valid phone number.</summary>
        /// <exception cref="InvalidOperationException"><paramref name="argument"/> is not a valid phone number.</exception>
        public static PhoneNumber Phone(string argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null) {
            if (argument is null) {
                throw new InvalidOperationException($"Parameter '{paramName}' cannot be null.");
            }
            if (!PhoneNumber.TryParse(argument, out var parsedPhone)) {
                throw new InvalidOperationException($"Invalid phone number format: {argument}");
            }
            return parsedPhone;
        }

        /// <summary>Throws an exception if the given string value is not a valid phone number.</summary>
        /// <exception cref="InvalidOperationException"><paramref name="argument"/> is not a valid phone number.</exception>
        public static Indice.Types.DevicePlatform DevicePlatform(string argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null) {
            if (argument is null) {
                throw new InvalidOperationException($"Parameter '{paramName}' cannot be null.");
            }
            if (Enum.TryParse<Indice.Types.DevicePlatform>(argument, out var platform)) {
                return platform;
            } else {
                throw new InvalidOperationException($"Unknown device platform: {argument}");
            }
        }
    }

    /// <summary>regular expression helper.</summary>
    protected static class RegexUtilities
    {
        /// <summary>Validates email addresses using a regular expression.</summary>
        public static bool IsValidEmail(string email) {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                static string DomainMapper(Match match) {
                    // Use IdnMapping class to convert Unicode domain names.
                    var idn = new IdnMapping();

                    // Pull out and process domain name (throws ArgumentException on invalid)
                    var domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                }
            } catch (RegexMatchTimeoutException) {
                return false;
            } catch (ArgumentException) {
                return false;
            }

            try {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            } catch (RegexMatchTimeoutException) {
                return false;
            }
        }
    }



    /// <summary>Request parameters for the <see cref="GuestGrantRequestParameterNames"/></summary>
    public static class GuestGrantRequestParameterNames
    {
        /// <summary>The client identifier.</summary>
        public const string ClientId = "client_id";
        /// <summary>The client secret.</summary>
        public const string DeviceId = "device_id";
        /// <summary>The device name.</summary>
        public const string DeviceName = "device_name";
        /// <summary>The device platform.</summary>
        public const string DevicePlatform = "device_platform";
        /// <summary>The push notification service handle.</summary>
        public const string PnsHandle = "pns_handle";
        /// <summary>The scope.</summary>
        public const string Scope = "scope";
        /// <summary>The given name / first name.</summary>
        public const string GivenName = "given_name";
        /// <summary>The family name / last name.</summary>
        public const string FamilyName = "family_name";
        /// <summary>The email address.</summary>
        public const string Email = "email";
        /// <summary>The phone number.</summary>
        public const string PhoneNumber = "phone_number";
    }
}
