using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using IdentityServer4.Models;

namespace Indice.AspNetCore.Identity.Scopes
{
    /// <summary>
    /// Extension methods on <see cref="DynamicScope"/>.
    /// </summary>
    public static class DynamicScopesExtensions
    {
        /// <summary>
        /// Test a scope name for patterns.
        /// </summary>
        /// <param name="scopeName"></param>
        public static bool IsPattern(this string scopeName) => !Regex.Escape(scopeName).Equals(scopeName);

        /// <summary>
        /// Tests a scope for patterns.
        /// </summary>
        /// <param name="scope"></param>
        public static bool HasRegex(this ApiScope scope) {
            if (scope == null) {
                throw new ArgumentNullException(nameof(scope));
            }
            return scope is DynamicScope || scope.Name.IsPattern();
        }

        /// <summary>
        /// Checks the scope. If this is a <see cref="DynamicScope"/> then check the metadata to see if it requires Strong customer authentication.
        /// </summary>
        /// <param name="scope"></param>
        public static bool RequiresSca(this ApiScope scope) {
            if (scope == null) {
                throw new ArgumentNullException(nameof(scope));
            }
            return (scope as DynamicScope)?.RequiresSca == true;
        }

        /// <summary>
        /// Convert the scope to regex.
        /// </summary>
        /// <param name="scope"></param>
        public static Regex AsPattern(this ApiScope scope) {
            if (scope == null) {
                throw new ArgumentNullException(nameof(scope));
            }
            if (scope is DynamicScope) {
                return new Regex(((DynamicScope)scope).Pattern);
            }
            return new Regex(scope.Name);
        }

        /// <summary>
        /// Filter <paramref name="availableScopes"/> for any of the requested names. This method takes into account <see cref="DynamicScope"/>.
        /// </summary>
        /// <param name="availableScopes"></param>
        /// <param name="requestedNames"></param>
        public static List<ApiScope> FindMatches(this IEnumerable<ApiScope> availableScopes, IEnumerable<string> requestedNames) {
            var matches = new List<ApiScope>();
            foreach (var name in requestedNames) {
                var exactMatch = availableScopes.Where(x => x.Name == name).FirstOrDefault();
                var match = exactMatch ?? availableScopes
                    .Where(apiScope => apiScope.HasRegex() && apiScope.AsPattern().IsMatch(name))
                    .Select(apiScope => new DynamicScope(name) {
                        Pattern = apiScope.Name,
                        Description = apiScope.Description,
                        Emphasize = apiScope.Emphasize,
                        DisplayName = apiScope.DisplayName,
                        Required = apiScope.Required,
                        ShowInDiscoveryDocument = apiScope.ShowInDiscoveryDocument,
                        UserClaims = apiScope.UserClaims,
                    })
                    .SingleOrDefault();
                if (match != null) {
                    matches.Add(match);
                }
            }
            return matches;
        }
    }
}