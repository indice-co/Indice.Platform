using NodaTime;
using NodaTime.TimeZones;

namespace Indice.Features.Identity.UI.Types;

/// <summary>
/// Contains information about TimeZones using the NodaTime tzdb.
/// </summary>
public sealed class ZoneInfo
{
    private ZoneInfo(string id,
                    string displayName,
                    TimeSpan baseUtcOffset,
                    string standardName,
                    string daylightName,
                    double latitude,
                    double longitude,
                    string countryCode) {
        Id = id;
        DisplayName = displayName;
        BaseUtcOffset = baseUtcOffset;
        StandardName = standardName;
        DaylightName = daylightName;
        Latitude = latitude;
        Longitude = longitude;
        CountryCode = countryCode;
    }

    /// <summary>
    /// Gets the time difference between the current time zone's standard time and Coordinated
    /// Universal Time (UTC).
    /// </summary>
    public TimeSpan BaseUtcOffset { get; }

    /// <summary>
    /// Gets the general display name that represents the time zone.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the time zone identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name for the time zone's standard time.
    /// </summary>
    public string StandardName { get; }

    /// <summary>
    /// Gets the display name for the time zone's dayight time.
    /// </summary>
    public string DaylightName { get; }

    /// <summary>
    /// Gets the latitude in degrees; positive for North, negative for South.
    /// </summary>
    /// <remarks>The value will be in the range [-90, 90].</remarks>
    /// <value>The latitude in degrees; positive for North, negative for South.</value>
    public double Latitude { get; }

    /// <summary>
    /// Gets the longitude in degrees; positive for East, negative for West.
    /// </summary>
    /// <remarks>The value will be in the range [-180, 180].</remarks>
    /// <value>The longitude in degrees; positive for East, negative for West.</value>
    public double Longitude { get; }

    /// <summary>
    /// Gets the ISO-3166 2-letter country code for the country containing the location.
    /// </summary>
    /// <value>The ISO-3166 2-letter country code for the country containing the location.</value>
    public string CountryCode { get; }

    /// <summary>
    /// Returns the DisplayName property.
    /// </summary>
    public override string ToString() {
        return DisplayName;
    }

    private static readonly Lazy<Dictionary<string, ZoneInfo>> timeZones;

    static ZoneInfo() {
        timeZones = new(static () => {
            var winter = Instant.FromUtc(DateTime.UtcNow.Year, 1, 1, 0, 0);
            var summer = Instant.FromUtc(DateTime.UtcNow.Year, 7, 1, 0, 0);
            var tzdb = DateTimeZoneProviders.Tzdb;

            var list =
            from location in TzdbDateTimeZoneSource.Default.ZoneLocations
            let zoneId = location.ZoneId
            let tz = tzdb[zoneId]
            let winterZoneInterval = tz.GetZoneInterval(winter)
            let summerZoneInterval = tz.GetZoneInterval(summer)
            orderby winterZoneInterval.StandardOffset, zoneId
            select new ZoneInfo(
                id: zoneId,
                displayName: $"(UTC{winterZoneInterval.StandardOffset.Milliseconds:+;-}{winterZoneInterval.StandardOffset:HH:mm}) {zoneId.Replace("_", " ")}",
                baseUtcOffset: winterZoneInterval.StandardOffset.ToTimeSpan(),
                standardName: winterZoneInterval.Savings.Milliseconds > 0 ? summerZoneInterval.Name : winterZoneInterval.Name,
                daylightName: summerZoneInterval.Savings.Milliseconds > 0 ? summerZoneInterval.Name : winterZoneInterval.Name,
                latitude: location.Latitude,
                longitude: location.Longitude,
                countryCode: location.CountryCode
            );
            return list.ToDictionary(x => x.Id);
        });
    }

    /// <summary>
    /// Returns a list of valid timezones.
    /// </summary>
    /// <param name="countryCode">
    /// The two-letter country code to get timezones for.
    /// Returns all timezones if null or empty.
    /// </param>
    public static IEnumerable<ZoneInfo> GetTimeZones(string? countryCode = null) {
        return countryCode is null
            ? timeZones.Value.Values
            : timeZones.Value.Values.Where(tz => tz.CountryCode == countryCode);
    }

    /// <summary>
    /// Returns a value indicationg whether the specified timezone id exists.
    /// </summary>
    /// <param name="timeZoneId">The tzdb id to search for.</param>
    /// <returns><see langword="true"/> if the id exists otherwise false.</returns>
    public static bool ContainsKey(string timeZoneId) => timeZones.Value.ContainsKey(timeZoneId);
}

