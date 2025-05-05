using System.Text.RegularExpressions;

namespace Indice.Features.Identity.Core.Models;

/// <summary>Models an MFA device identifier.</summary>
/// <param name="Value">The device id.</param>
/// <param name="RegistrationId">The device registration id.</param>
public sealed partial record MfaDeviceIdentifier(string? Value, Guid? RegistrationId = null)
{
    private static readonly Regex _deviceIdFormat = DevideIdentifierFormat();
    private static readonly MfaDeviceIdentifier _empty = new(string.Empty, null);

    /// <summary>Determines if there is a value for <see cref="RegistrationId"/>.</summary>
    public bool HasRegistrationId => RegistrationId.HasValue;

    /// <summary>Check for empty value</summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    /// <summary>
    /// Empty MfaDeviceIdentifier
    /// </summary>
    public static MfaDeviceIdentifier Empty => _empty;

    /// <summary>
    /// Validates the device id.
    /// </summary>
    /// <param name="deviceId">The input device id as a string</param>
    /// <returns>True if the device id is valid. Otherwize false</returns>
    /// <remarks>
    /// the device id can be either one of the following:
    /// <list type="bullet">
    ///   <item>valid guid (using Guid.TryParse)</item>
    ///   <item>a valid browser fingerprint format `{hash128}.{browerName}`.
    ///     Usually fingerprint hash is a sha256 that is 64 characters long in hex string representation.
    ///     Also in order to avoid colisions with same browser engine different vendors
    ///     we suffix the browser fingerprint with the browser name separated with the dot `.` character.
    ///   </item>
    /// </list>
    /// </remarks>
    public static bool ValidateDeviceId(string? deviceId) {
        // Do the flollowing checks:
        // - check for empty
        // - check for null
        // - check format can be either one of the following
        //   - valid guid (using Guid.TryParse)
        //   - a valid browser fingerprint format `{hash128}.{browerName}`.
        //     Usually fingerprint hash is a md5 that is 32 characters long in hex string representation.
        //     Also in order to avoid colisions with same browser engine different vendors
        //     we suffix the browser fingerprint with the browser name separated with the dot `.` character.
        return !string.IsNullOrWhiteSpace(deviceId) &&
               (Guid.TryParse(deviceId, out _) ||
                _deviceIdFormat.IsMatch(deviceId));
    }

    [GeneratedRegex(@"^([a-fA-F0-9]{32})(\.[a-zA-Z0-9\-]+)?$")]
    private static partial Regex DevideIdentifierFormat();

    /// <inheritdoc />
    public override int GetHashCode() => Value?.GetHashCode() ?? string.Empty.GetHashCode();

    /// <inheritdoc />
    public bool Equals(MfaDeviceIdentifier? other) => GetHashCode().Equals(other?.GetHashCode() ?? -1);
}
