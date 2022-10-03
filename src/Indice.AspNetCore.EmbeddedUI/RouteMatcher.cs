using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Indice.AspNetCore.EmbeddedUI
{
    internal class RouteMatcher
    {
        public RouteMatcher(HttpContext httpContext) {
            HttpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
        }

        public HttpContext HttpContext { get; }

        public bool IsMatch(RoutePattern pattern, out IDictionary<string, string> resolvedParameters) {
            resolvedParameters = new Dictionary<string, string>();
            if (pattern.RawText.Equals("/"))
                return true;
            var requestPath = HttpContext.Request.Path.Value;
            var isValid = Uri.TryCreate($"https://example.com/{requestPath.Trim('/')}", UriKind.Absolute, out var requestUri);
            if (!isValid) {
                return false;
            }
            var patternSegments = pattern.PathSegments.Select(segment => segment.Parts.First());
            var requestUriSegments = requestUri
                .Segments
                .Where(segment => !segment.Equals("/"))
                .Select(segment => segment.Trim('/'));
            if (requestUriSegments.Count() < patternSegments.Count()) {
                return false;
            }
            var isMatch = false;
            var patternParameters = pattern.Parameters.Select(parameter => "{" + parameter.Name + "}");
            for (var index = 0; index < requestUriSegments.Count(); index++) {
                var segment = requestUriSegments.ElementAt(index);
                var patternSegment = patternSegments.ElementAtOrDefault(index);
                if (patternSegment?.IsParameter == false) {
                    isMatch = segment.Equals((patternSegment as RoutePatternLiteralPart)?.Content, StringComparison.OrdinalIgnoreCase);
                    if (!isMatch) {
                        break;
                    }
                    continue;
                }
                if (patternSegment is not null) {
                    var parameterName = (patternSegment as RoutePatternParameterPart)?.Name;
                    resolvedParameters.Add(parameterName, segment);
                }
            }
            return isMatch;
        }
    }
}
