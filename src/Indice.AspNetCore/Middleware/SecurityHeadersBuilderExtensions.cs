using Indice.AspNetCore.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods on the <see cref="IApplicationBuilder"/>.
/// Adds the Security Headers Policy for the following headers in the response.<br />
/// <strong>Content-Security-Policy</strong>, <strong>X-Frame-Options</strong>, <strong>Referrer-Policy</strong>, <strong>X-Content-Type-Options</strong>.
/// </summary>
public static class SecurityHeadersBuilderExtensions
{
    /// <summary>Adds the <see cref="SecurityHeadersMiddleware"/> to the pipeline.</summary>
    /// <param name="builder">The application pipeline builder.</param>
    /// <param name="configurePolicy">Only use this here in case of different policy configuration (Than what whas added by <em>services.AddSecurityHeaders()</em>)</param>
    /// <returns>The input builder for further configuration.</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder, Action<SecurityHeadersPolicy> configurePolicy = null) {
        var policy = builder.ApplicationServices.GetService<SecurityHeadersPolicy>();
        policy = policy is not null ? new SecurityHeadersPolicy {
            ContentSecurityPolicy = policy.ContentSecurityPolicy.Clone(),
            ReferrerPolicy = policy.ReferrerPolicy,
            XContentTypeOptions = policy.XContentTypeOptions,
            XFrameOptions = policy.XFrameOptions,
        } : new SecurityHeadersPolicy();
        configurePolicy?.Invoke(policy);
        builder.UseMiddleware<SecurityHeadersMiddleware>(policy);
        return builder;
    }
}
