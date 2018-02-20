using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;

namespace Indice.AspNetCore.TagHelpers
{
    public static class TagHelperExtensions
    {
        internal static RouteValueDictionary ToRouteValueDictionary(this IEnumerable<KeyValuePair<string, string>> routeValues) {
            var values = new RouteValueDictionary();
            foreach (var item in routeValues) {
                values.Add(item.Key, item.Value);
            }
            return values;
        }
    }
}
