using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Extensions.Localization;

/// <summary>
/// Extensions on the <see cref="IStringLocalizer"/>
/// </summary>
public static class IStringLocalizerExtensions
{
    /// <summary>
    /// Gets the list of all resource string translations for the given culture info.
    /// </summary>
    /// <param name="stringLocalizer">The string localizer</param>
    /// <param name="culture">The culture for which to get the strings.</param>
    /// <param name="includeParentCultures">A bool indicating whether to include string from parent cultures.</param>
    /// <returns></returns>
    public static IEnumerable<LocalizedString> GetAllStrings(this IStringLocalizer stringLocalizer, CultureInfo culture, bool includeParentCultures = true) {
        if (stringLocalizer is null) {
            throw new ArgumentNullException(nameof(stringLocalizer));
        }

        if (culture is null) {
            throw new ArgumentNullException(nameof(culture));
        }
        var origCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        var strings = stringLocalizer.GetAllStrings(includeParentCultures);
        CultureInfo.CurrentCulture = origCulture;
        CultureInfo.CurrentUICulture = origCulture;
        return strings;
    }
}
