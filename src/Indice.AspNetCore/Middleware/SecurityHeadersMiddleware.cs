using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Middleware
{
    /// <summary>
    /// Adds the Security Headers Policy for the following headers in the response.<br />
    /// <strong>Content-Security-Policy</strong>, <strong>X-Frame-Options</strong>, <strong>Referrer-Policy</strong>, <strong>X-Content-Type-Options</strong>.
    /// </summary>
    internal class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SecurityHeadersPolicy _securityHeadersPolicy;

        public SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersPolicy securityHeadersPolicy) {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _securityHeadersPolicy = securityHeadersPolicy;
        }


        /// <summary>Invokes the middleware and tries to find the correct file to serve.</summary>
        /// <param name="context">The HTTP context.</param>
        public async Task InvokeAsync(HttpContext context) {
            context.ApplySecurityHeaders(_securityHeadersPolicy);
            await _next.Invoke(context);
        }
    }
}
