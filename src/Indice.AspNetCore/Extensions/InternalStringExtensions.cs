using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Indice.AspNetCore.Extensions
{
    /// <summary>
    /// Useful extensions on <see cref="string"/> type.
    /// </summary>
    internal static class InternalStringExtensions
    {
        /// <summary>
        /// Ensures that a given path is prefixed with a '/' character.
        /// </summary>
        /// <param name="path">The given path.</param>
        public static string EnsureLeadingSlash(this string path) => $"/{path.TrimStart('/')}";

        /// <summary>
        /// Checks if a path contains a dynamic part (e.g. /users/{userId}/block).
        /// </summary>
        /// <param name="path">The given path.</param>
        public static bool IsDynamicPath(this string path) {
            var isDynamicPath = path.IsDynamicPath(out _);
            return isDynamicPath;
        }

        /// <summary>
        /// Transforms a dynamic path into a templated one, used by application's internal structs.
        /// </summary>
        /// <param name="path">The given path.</param>
        public static string ToTemplatedDynamicPath(this string path) {
            var isDynamicPath = path.IsDynamicPath(out var matches);
            if (!isDynamicPath) {
                return path;
            }
            foreach (var match in matches) {
                path = path.Replace(match, "**");
            }
            return path;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ignoredPaths"></param>
        /// <param name="path"></param>
        /// <param name="queryString"></param>
        /// <param name="httpMethod"></param>
        /// <returns></returns>
        public static bool IsIgnoredPath(IDictionary<string, string> ignoredPaths, string path, string queryString, string httpMethod) {
            path = path + queryString;
            // Check if specified path matches exactly an ignored path.
            var isExactMatch = ignoredPaths.ContainsKey(path) && (string.IsNullOrWhiteSpace(httpMethod) || ignoredPaths[path].Split('|').Any(method => method.Equals(httpMethod, StringComparison.OrdinalIgnoreCase)));
            if (isExactMatch) {
                return true;
            }
            // Check if specified path is a subpath of an ignored path.
            var paths = ignoredPaths.Where(p => path.StartsWith(p.Key));
            if (paths.Any()) {
                // Order found paths in ascending order, based on size. 
                var basePath = paths.OrderBy(x => x.Key.Length).First().Key;
                var isSubPath = ignoredPaths.ContainsKey(basePath) && (ignoredPaths[basePath].Split('|').Any(method => method.Equals(httpMethod, StringComparison.OrdinalIgnoreCase)) || ignoredPaths[basePath].Equals("*"));
                if (isSubPath) {
                    return true;
                }
            }
            // Check if specified dynamic path is an exact match of an ignored path.
            var pathParts = path.TrimStart('/').Split('/');
            var segmentsLength = pathParts.Length;
            var sameSizePaths = ignoredPaths.Where(path =>
                path.Key.TrimStart('/').Split('/').Length == segmentsLength &&
                (path.Value.Equals("*") || path.Value.Split('|').Any(method => method.Equals(httpMethod, StringComparison.OrdinalIgnoreCase)))
            );
            if (!sameSizePaths.Any()) {
                return false;
            }
            var dynamicMatchFound = false;
            foreach (var sameSizePath in sameSizePaths) {
                var matchFound = false;
                var comparingPathParts = sameSizePath.Key.TrimStart('/').Split('/');
                for (var i = 0; i < comparingPathParts.Length; i++) {
                    var currentSegment = comparingPathParts[i];
                    if (currentSegment.Equals("**")) {
                        continue;
                    }
                    if (!currentSegment.Equals(pathParts[i], StringComparison.OrdinalIgnoreCase)) {
                        matchFound = false;
                        break;
                    }
                    matchFound = true;
                }
                dynamicMatchFound = matchFound;
                if (dynamicMatchFound) {
                    break;
                }
            }
            return dynamicMatchFound;
        }

        private static bool IsDynamicPath(this string path, out IList<string> matches) {
            matches = new List<string>();
            var regex = new Regex("{(.*?)}");
            var match = regex.Match(path);
            var isDynamicPath = match.Success;
            if (isDynamicPath) {
                matches.Add(match.Value);
                var nextMatch = match.NextMatch();
                var hasNextMatch = nextMatch.Success;
                while (hasNextMatch) {
                    nextMatch = nextMatch.NextMatch();
                    hasNextMatch = nextMatch.Success;
                    matches.Add(nextMatch.Value);
                }
            }
            return isDynamicPath;
        }
    }
}