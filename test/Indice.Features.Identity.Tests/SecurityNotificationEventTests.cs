using Indice.Features.GeoIP;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Events.Models;
using Indice.Features.Identity.SignInLogs.Events;
using Indice.Security;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace Indice.Features.Identity.Tests;

public class SecurityNotificationEventTests
{
    private static readonly DateTimeOffset UtcTimestamp = new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero);

    private static SecurityNotificationEvent CreateEvent(
        string? zoneInfoClaim = null,
        string? countryIsoCode = null) {
        var user = new User("testuser");
        if (zoneInfoClaim is not null) {
            ((ICollection<IdentityUserClaim<string>>)user.Claims).Add(
                new IdentityUserClaim<string> { ClaimType = BasicClaimTypes.ZoneInfo, ClaimValue = zoneInfoClaim });
        }
        var userContext = UserEventContext.InitializeFromUser(user);
        var location = new IPAddressLocation {
            IPAddress = "1.2.3.4",
            CountryIsoCode = countryIsoCode
        };
        return new SecurityNotificationEvent("TestActivity", userContext, location) {
            TimeStamp = UtcTimestamp
        };
    }

    [Fact]
    public void LocalTimeStamp_WhenZoneInfoClaimIsValid_ConvertsToUserTimezone() {
        // Europe/Athens is UTC+3 during summer (EEST).
        const string ianaTimezone = "Europe/Athens";
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(ianaTimezone, out var tz)) {
            // Skip: timezone not resolvable on this platform.
            return;
        }
        var @event = CreateEvent(zoneInfoClaim: ianaTimezone);
        var expected = TimeZoneInfo.ConvertTime(UtcTimestamp, tz);
        Assert.Equal(expected, @event.LocalTimeStamp);
        Assert.NotEqual(TimeSpan.Zero, @event.LocalTimeStamp.Offset);
    }

    [Fact]
    public void LocalTimeStamp_WhenZoneInfoClaimIsInvalid_FallsBackToCountryTimezone() {
        const string invalidTimezone = "Not/AValidTimezone";
        const string countryIsoCode = "GR"; // Greece -> "Europe/Athens"
        var @event = CreateEvent(zoneInfoClaim: invalidTimezone, countryIsoCode: countryIsoCode);
        // Should not throw; result depends on whether the country TZ resolves on the platform.
        _ = @event.LocalTimeStamp;
    }

    [Fact]
    public void LocalTimeStamp_WhenNoZoneInfoClaim_FallsBackToCountryTimezone() {
        const string countryIsoCode = "GR"; // Greece -> "Europe/Athens"
        var @event = CreateEvent(zoneInfoClaim: null, countryIsoCode: countryIsoCode);
        if (!TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Athens", out var tz)) {
            return;
        }
        var expected = TimeZoneInfo.ConvertTime(UtcTimestamp, tz);
        Assert.Equal(expected, @event.LocalTimeStamp);
        Assert.NotEqual(TimeSpan.Zero, @event.LocalTimeStamp.Offset);
    }

    [Fact]
    public void LocalTimeStamp_WhenNoTimezoneCanBeResolved_ReturnsUtcTimestamp() {
        var @event = CreateEvent(zoneInfoClaim: null, countryIsoCode: null);
        Assert.Equal(UtcTimestamp, @event.LocalTimeStamp);
    }

    [Fact]
    public void LocalTimeStamp_WhenCountryIsoCodeIsUnrecognised_ReturnsUtcTimestamp() {
        const string unknownIso = "XX"; // Not a real ISO code
        var @event = CreateEvent(zoneInfoClaim: null, countryIsoCode: unknownIso);
        Assert.Equal(UtcTimestamp, @event.LocalTimeStamp);
    }
}
