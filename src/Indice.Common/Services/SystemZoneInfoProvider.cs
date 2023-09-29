using Indice.Types;

namespace Indice.Services;

/// <summary>
/// A default implementation of <see cref="IZoneInfoProvider"/> that
/// uses the default dotnet <see cref="TimeZoneInfo"/> object.
/// </summary>
/// <remarks>This returns different objects for Windows and Linux.</remarks>
public class SystemZoneInfoProvider : IZoneInfoProvider
{
    private ZoneInfo[] _zoneInfos;

    /// <inheritdoc/>
    public IEnumerable<ZoneInfo> GetTimeZones() {

        _zoneInfos ??= TimeZoneInfo
            .GetSystemTimeZones()
            .Select(tz => new ZoneInfo(
                id: tz.Id,
                displayName: tz.DisplayName,
                baseUtcOffset: tz.BaseUtcOffset,
                standardName: tz.StandardName,
                daylightName: tz.DaylightName
            ))
            .ToArray();

        return _zoneInfos;
    }
}
