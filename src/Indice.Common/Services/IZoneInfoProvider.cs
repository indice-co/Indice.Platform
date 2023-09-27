using Indice.Types;

namespace Indice.Services;

/// <summary>
/// Timezone abstraction in order to support different timezone providers (eg: System, NodaTime etc).
/// </summary>
public interface IZoneInfoProvider
{
    /// <summary>
    /// Returns a list of valid timezones.
    /// </summary>
    IEnumerable<ZoneInfo> GetTimeZones();
}
