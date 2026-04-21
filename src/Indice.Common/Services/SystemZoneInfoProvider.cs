using System.Globalization;
using System.Resources;
using Indice.Translations;
using Indice.Types;

namespace Indice.Services;

/// <summary>
/// A default implementation of <see cref="IZoneInfoProvider"/> that
/// uses the default dotnet <see cref="TimeZoneInfo"/> object.
/// </summary>
/// <remarks>This returns different objects for Windows and Linux.</remarks>
public class SystemZoneInfoProvider : IZoneInfoProvider
{
    private readonly ResourceManager? _resourceManager;

    /// <summary>
    /// Initializes a new instance of <see cref="SystemZoneInfoProvider"/>.
    /// </summary>
    public SystemZoneInfoProvider() : this(null) { }

    /// <summary>
    /// Initializes a new instance of <see cref="SystemZoneInfoProvider"/> with an optional <see cref="ResourceManager"/> for localizing timezone display names.
    /// </summary>
    /// <param name="resourceManager">The resource manager containing timezone translations. If null, the default <see cref="TimeZones"/> resource manager is used.</param>
    public SystemZoneInfoProvider(ResourceManager? resourceManager) {
        _resourceManager = resourceManager ?? TimeZones.ResourceManager;
    }

    /// <inheritdoc/>
    public IEnumerable<ZoneInfo> GetTimeZones() {
        return TimeZoneInfo
            .GetSystemTimeZones()
            .Select(tz => new ZoneInfo(
                id: tz.Id,
                displayName: GetLocalizedDisplayName(tz.Id, tz.DisplayName),
                baseUtcOffset: tz.BaseUtcOffset,
                standardName: tz.StandardName,
                daylightName: tz.DaylightName,
                systemDisplayName: tz.DisplayName
            ))
            .ToArray();
    }

    private string GetLocalizedDisplayName(string timezoneId, string fallbackDisplayName) {
        var localizedName = _resourceManager?.GetString(timezoneId, CultureInfo.CurrentUICulture);
        return string.IsNullOrEmpty(localizedName) ? fallbackDisplayName : localizedName;
    }
}
