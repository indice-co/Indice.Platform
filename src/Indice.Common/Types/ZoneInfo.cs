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
        string displayName,
        TimeSpan baseUtcOffset,
        string standardName,
        string daylightName
    ) {
        Id = id;
        DisplayName = displayName;
        BaseUtcOffset = baseUtcOffset;
        StandardName = standardName;
        DaylightName = daylightName;
    }

    /// <summary>
    /// Gets the time zone identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the general display name that represents the time zone.
    /// </summary>
    public string DisplayName { get; }

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
}