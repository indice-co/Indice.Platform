using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides helper functions for Authenication middleware forwarding logic.
    /// </summary>
    public static class BearerSelector
    {
        /// <summary>
        /// Provides a forwarding func for JWT vs reference tokens (based on existence of dot in token).
        /// </summary>
        /// <param name="introspectionScheme">Scheme name of the introspection handler.</param>
        public static Func<HttpContext, string> ForwardReferenceToken(string introspectionScheme = "introspection") {
            string Select(HttpContext context) {
                var (scheme, credential) = GetSchemeAndCredential(context);
                if (scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase) &&
                    !credential.Contains(".")) {
                    return introspectionScheme;
                }
                return null;
            }
            return Select;
        }

        /// <summary>
        /// Extracts scheme and credential from Authorization header (if present).
        /// </summary>
        /// <param name="context">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
        public static (string, string) GetSchemeAndCredential(HttpContext context) {
            var header = context.Request.Headers[HeaderNames.Authorization].FirstOrDefault();
            if (string.IsNullOrEmpty(header)) {
                return (string.Empty, string.Empty);
            }
            var parts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) {
                return (string.Empty, string.Empty);
            }
            return (parts[0], parts[1]);
        }
    }
}
