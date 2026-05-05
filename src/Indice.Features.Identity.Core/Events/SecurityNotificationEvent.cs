using Indice.Events;
using Indice.Features.GeoIP;
using Indice.Features.Identity.Core.Events.Models;
using Indice.Globalization;
using Indice.Security;

namespace Indice.Features.Identity.SignInLogs.Events;
/// <summary>
/// Represents the data model used for constructing an email notification when a user's password is changed.
/// </summary>
/// <remarks>This model contains information about the user, the device that initiated the password change (if
/// applicable), and additional details such as the email subject and display name.</remarks>
public class SecurityNotificationEvent : IPlatformEvent
{
    /// <summary>Initializes a new instance of the <see cref="SecurityNotificationEvent"/> class.</summary>
    public SecurityNotificationEvent(string activity, UserEventContext user, IPAddressLocation location) {
        Activity = activity ?? throw new ArgumentNullException(nameof(activity));
        User = user ?? throw new ArgumentNullException(nameof(user));
        Location = location ?? throw new ArgumentNullException(nameof(user));
    }
    /// <summary>The activty that triggered the security notification.</summary>
    public string Activity { get; set; } = null!;
    /// <summary>The user instance.</summary>
    public UserEventContext User { get; set; } = null!;
    /// <summary>The user's email address.</summary>
    public IPAddressLocation Location { get; set; } = null!;
    /// <summary>The device metadata.</summary>
    public DeviceEventContext Device { get; set; } = DeviceEventContext.FromUserAgent(null);
    /// <summary>The device that initiated the password change, if any.</summary>
    public UserDeviceEventContext? UserDevice { get; set; }
    /// <summary>The client that initiated the password change, if any.</summary>
    public ClientEventContext? Client { get; set; }
    /// <summary>Gets or sets the timestamp indicating when the event occurred.</summary>
    public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>The translated Subject of the message.</summary>
    public string? Subject { get; set; }
    /// <summary>The translated Body of the message.</summary>
    public string? Description { get; set; } = null;
    /// <summary>The users preferred locale</summary>
    public string? Locale { get; set; }

    /// <summary>
    /// Gets the timestamp converted to the user's local timezone.
    /// First attempts to resolve the timezone from the user's <c>zoneinfo</c> claim.
    /// Falls back to the timezone derived from <see cref="Location"/>.<see cref="IPAddressLocation.CountryIsoCode"/> if the claim is unavailable.
    /// Returns <see cref="TimeStamp"/> unchanged if neither source yields a valid timezone.
    /// </summary>
    public DateTimeOffset LocalTimeStamp {
        get {
            // 1. Try to get timezone from user ZoneInfo claim.
            var zoneInfoClaim = User?.Claims?.FirstOrDefault(c => c.Type == BasicClaimTypes.ZoneInfo);
            if (!string.IsNullOrEmpty(zoneInfoClaim?.Value) &&
                TimeZoneInfo.TryFindSystemTimeZoneById(zoneInfoClaim.Value, out var tzFromClaim)) {
                return TimeZoneInfo.ConvertTime(TimeStamp, tzFromClaim);
            }
            // 2. Fallback: derive timezone from the location's country ISO code.
            if (Location?.CountryIsoCode is not null &&
                CountryInfo.TryGetCountryByNameOrCode(Location.CountryIsoCode, out var country) &&
                country is not null &&
                TimeZoneInfo.TryFindSystemTimeZoneById(country.TimeZoneId, out var tzFromCountry)) {
                return TimeZoneInfo.ConvertTime(TimeStamp, tzFromCountry);
            }
            // 3. Return UTC timestamp unchanged when no timezone can be resolved.
            return TimeStamp;
        }
    }
}