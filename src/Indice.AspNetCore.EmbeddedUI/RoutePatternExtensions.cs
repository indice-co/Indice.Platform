using System.Collections.Generic;

namespace Microsoft.AspNetCore.Routing.Patterns
{
    internal static class RoutePatternExtensions
    {
        public static string GetRequestPath(this RoutePattern pattern, IDictionary<string, string> parameterValues) {
            var requestPath = '/' + pattern.RawText.Trim('/');
            foreach (var parameter in parameterValues) {
                requestPath = requestPath.Replace("{" + parameter.Key + "}", parameter.Value);
            }
            return requestPath;
        }
    }
}
