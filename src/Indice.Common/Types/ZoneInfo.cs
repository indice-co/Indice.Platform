using System.Globalization;
namespace Indice.Types;

/// <summary>
/// Contains information about a TimeZone.
/// </summary>
public class ZoneInfo
{
    /// <summary>
    /// Initialize a new instance of <see cref="ZoneInfo"/>.
    /// </summary>
    public ZoneInfo(
        string id,
        string systemDisplayName,
        TimeSpan baseUtcOffset,
        string standardName,
        string daylightName
    ) {
        Id = id;
        BaseUtcOffset = baseUtcOffset;
        StandardName = standardName;
        DaylightName = daylightName;
        SystemDisplayName = systemDisplayName;
    }

    /// <summary>
    /// Gets the time zone identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the translated display name that represents the time zone.
    /// </summary>
    public string DisplayName => GetLocalizedDisplayName(Id, SystemDisplayName);

    /// <summary>
    /// Gets the system display name that represents the time zone.
    /// </summary>
    public string SystemDisplayName { get; }

    /// <summary>
    /// Gets the time difference between the current time zone's standard time and Coordinated
    /// Universal Time (UTC).
    /// </summary>
    public TimeSpan BaseUtcOffset { get; }

    /// <summary>
    /// Gets the display name for the time zone's standard time.
    /// </summary>
    public string StandardName { get; }

    /// <summary>
    /// Gets the display name for the time zone's dayight time.
    /// </summary>
    public string DaylightName { get; }

    /// <summary>
    /// Returns the <see cref="DisplayName"/> property.
    /// </summary>
    public override string ToString() {
        return DisplayName;
    }

    /// <summary>
    /// Helper function to retrieve the display name in the user's locale, if available. If not, it falls back to the provided display name.
    /// </summary>
    /// <param name="timezoneId">The identifier of the time zone.</param>
    /// <param name="fallbackDisplayName">The display name to use if a localized name is not available.</param>
    /// <returns>The localized display name if available; otherwise, the fallback display name.</returns>
    private static string GetLocalizedDisplayName(string timezoneId, string fallbackDisplayName) {
        var localizedName = TimeZones.ZoneInfoTranslations.ResourceManager.GetString(timezoneId, CultureInfo.CurrentUICulture);
        return string.IsNullOrEmpty(localizedName) ? fallbackDisplayName : localizedName;
    }
}