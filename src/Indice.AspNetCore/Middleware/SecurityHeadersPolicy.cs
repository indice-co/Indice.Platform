namespace Indice.AspNetCore.Middleware;

/// <summary>The <see cref="SecurityHeadersPolicy"/> is the configuration options for the <seealso cref="SecurityHeadersMiddleware"/>.</summary>
public class SecurityHeadersPolicy
{
    /// <summary>Adds the <strong>X-Content-Type-Options</strong> header.. Defaults to <em>nosniff</em></summary>
    /// <remarks><a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Content-Type-Options">MDN: X-Content-Type-Options</a></remarks>
    public string XContentTypeOptions { get; set; }  = "nosniff";
    /// <summary>Adds the <strong>Referrer-Policy</strong> header. Defaults to <em>strict-origin-when-cross-origin</em>.</summary>
    /// <remarks><a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Referrer-Policy">MDN: Referrer-Policy</a></remarks>
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";
    /// <summary>Adds the <strong>X-Frame-Options</strong> header. Defaults to <em>SAMEORIGIN</em>.</summary>
    /// <remarks><a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Frame-Options">MDN: X-Frame-Options</a></remarks>
    public string XFrameOptions { get; set; } = "SAMEORIGIN";
    /// <summary>Adds the <strong>Content-Security-Policy</strong> (HTML only)</summary>
    public CSP ContentSecurityPolicy { get; set; } = CSP.DefaultPolicy.Clone();
    /// <summary>Checks the existence of the <see cref="XContentTypeOptions"/> setting.</summary>
    public bool HasXContentTypeOptions => !string.IsNullOrWhiteSpace(XContentTypeOptions);
    /// <summary>Checks the existence of the <see cref="ReferrerPolicy"/> setting.</summary>
    public bool HasReferrerPolicy => !string.IsNullOrWhiteSpace(ReferrerPolicy);
    /// <summary>Checks the existence of the <see cref="XFrameOptions"/> setting.</summary>
    public bool HasXFrameOptions => !string.IsNullOrWhiteSpace(XFrameOptions);
    /// <summary>Checks the existence of the <see cref="ContentSecurityPolicy"/> setting.</summary>
    public bool HasContentSecurityPolicy => ContentSecurityPolicy is not null;
}
