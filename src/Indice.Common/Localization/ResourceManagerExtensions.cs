#nullable enable
using System.Collections;
using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Indice.Localization;
/// <summary>
/// Extensions on the <see cref="System.Resources.ResourceManager"/>
/// </summary>
public static class ResourceManagerExtensions
{
    /// <summary>
    /// Creates an object graph of nested <em>Dictionary&lt;string, object&gt;</em> 
    /// equivalent to the flat structure represented by the keys of the resource strings inside the <paramref name="resourceManager"/>
    /// </summary>
    /// <remarks>Can be serialized and returned as json for client side localization</remarks>
    /// <param name="resourceManager">The resource manager that we need to export</param>
    /// <param name="cultureInfo">The culture to export keys for.</param>
    /// <param name="pathDelimiter">The delimiter character for determining a path from a given key. For example for key &quot;feature.dashboard.title&quot; would be '.' </param>
    /// <returns>The object graph in the form of a <em>Dictionary&lt;string, object&gt;</em></returns>
    public static Dictionary<string, object> ToObjectGraph(this System.Resources.ResourceManager resourceManager, CultureInfo? cultureInfo = null, char pathDelimiter = '.') {
        var set = resourceManager.GetResourceSet(cultureInfo ?? CultureInfo.CurrentUICulture, createIfNotExists: true, tryParents: true)!;
        var strings = set.Cast<DictionaryEntry>().Select(x => new LocalizedString(x.Key.ToString()!, set.GetString(x.Key.ToString()!)!));
        return IStringLocalizerExtensions.ToObjectGraph(strings, pathDelimiter);
    }
}
#nullable disable