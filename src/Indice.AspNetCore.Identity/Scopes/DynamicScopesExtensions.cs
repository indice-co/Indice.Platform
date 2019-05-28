using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using IdentityServer4.Models;

namespace Indice.AspNetCore.Identity.Scopes
{
    /// <summary>
    /// Extension methods on <see cref="DynamicScope"/>
    /// </summary>
    public static class DynamicScopesExtensions
    {
        /// <summary>
        /// test a scope name for patterns
        /// </summary>
        /// <param name="scopeName"></param>
        /// <returns></returns>
        public static bool IsPattern(this string scopeName) {
            return !Regex.Escape(scopeName).Equals(scopeName);
        }

        /// <summary>
        /// Tests a scope for patterns
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        public static bool HasRegex(this Scope scope) {
            if (scope == null) {
                throw new ArgumentNullException(nameof(scope));
            }
            return scope is DynamicScope || scope.Name.IsPattern();
        }

        /// <summary>
        /// Checks the Scope. If this is a <see cref="DynamicScope"/> then check the metadata 
        /// to see if it requires Strong customer authentication.
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        public static bool RequiresSca(this Scope scope) {
            if (scope == null) {
                throw new ArgumentNullException(nameof(scope));
            }
            return (scope as DynamicScope)?.RequiresSca == true;
        }


        /// <summary>
        /// Convert the scope to regex.
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        public static Regex AsPattern(this Scope scope) {
            if (scope == null) {
                throw new ArgumentNullException(nameof(scope));
            }
            if (scope is DynamicScope)
                return new Regex(((DynamicScope)scope).Pattern);
            return new Regex(scope.Name);
        }

        /// <summary>
        /// Filter <paramref name="availableScopes"/> for any of the requested names. This method takes into account <see cref="DynamicScope"/>
        /// </summary>
        /// <param name="availableScopes"></param>
        /// <param name="requestedNames"></param>
        /// <returns></returns>
        public static List<Scope> FindMatches(this IEnumerable<Scope> availableScopes, IEnumerable<string> requestedNames) {
            var matches = new List<Scope>();
            foreach (var name in requestedNames) {
                var exactmatch = availableScopes.Where(x => x.Name == name).FirstOrDefault(); // exact match
                var match = exactmatch ?? availableScopes.Where(s => s.HasRegex() && s.AsPattern().IsMatch(name))
                                       .Select(s => new DynamicScope(name) {
                                                        Pattern = s.Name,
                                                        Description = s.Description,
                                                        Emphasize = s.Emphasize,
                                                        DisplayName = s.DisplayName,
                                                        Required = s.Required,
                                                        ShowInDiscoveryDocument = s.ShowInDiscoveryDocument,
                                                        UserClaims = s.UserClaims,
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