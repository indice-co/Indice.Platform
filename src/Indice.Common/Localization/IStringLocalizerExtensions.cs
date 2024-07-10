#nullable enable
using System.Globalization;

namespace Microsoft.Extensions.Localization;

/// <summary>Extensions on the <see cref="IStringLocalizer"/></summary>
public static class IStringLocalizerExtensions
{
    /// <summary>Gets the list of all resource string translations for the given culture info.</summary>
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

    /// <summary>
    /// Creates an object graph of nested <em>Dictionary&lt;string, object&gt;</em> 
    /// equivalent to the flat structure represented by the keys of the resource strings inside the <paramref name="resourceManager"/>
    /// </summary>
    /// <remarks>Can be serialized and returned as json for client side localization</remarks>
    /// <param name="stringLocalizer">The string localizer we need to export</param>
    /// <param name="culture">The culture to export keys for.</param>
    /// <param name="pathDelimiter">The delimiter character for determining a path from a given key. For example for key &quot;feature.dashboard.title&quot; would be '.' </param>
    /// <returns>The object graph in the form of a <em>Dictionary&lt;string, object&gt;</em></returns>
    public static Dictionary<string, object> ToObjectGraph(this IStringLocalizer stringLocalizer, CultureInfo culture, char pathDelimiter = '.') =>
        stringLocalizer.GetAllStrings(culture, includeParentCultures: true).ToObjectGraphInternal(pathDelimiter);

    internal static Dictionary<string, object> ToObjectGraphInternal(this IEnumerable<LocalizedString> strings, char pathDelimiter = '.') {
        var graph = new Dictionary<string, object>();
        foreach (var keyValue in strings) {
            var keyPath = keyValue.Name.Split(pathDelimiter)!;
            var depth = 0;
            var node = graph;
            // ensure path;
            while (depth < keyPath.Length) {
                var subKey = keyPath[depth];
                if (!node.ContainsKey(subKey) && depth < keyPath.Length - 1) {
                    node.Add(subKey, new Dictionary<string, object>());
                }
                if (depth < keyPath.Length - 1) {
                    node = (Dictionary<string, object>)node[subKey];
                } else {
                    var value = keyValue.Value;
                    node.Add(keyPath[depth], value);
                    break;
                }
                depth++;
            }
        }
        return graph;
    }
}
#nullable disable