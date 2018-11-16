using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace Indice.AspNetCore.Identity.Filters
{
    /// <summary>
    /// Sets the Content Security policy header for the current action.
    /// </summary>
    public class SecurityHeadersAttribute : ActionFilterAttribute
    {
        private readonly CSP _csp;

        /// <summary>
        /// Constractor Defaults to allowing self origin plus google for fonts and scripts (google cdn) and wildcard for images.
        /// </summary>
        public SecurityHeadersAttribute() : this(null) { }

        /// <summary>
        /// Constractor with custom security policy.
        /// </summary>
        public SecurityHeadersAttribute(CSP scp) {
            _csp = scp ?? new CSP {
                DefaultSrc = $"{CSP.Self}",
                ScriptSrc = $"{CSP.Self} ajax.googleapis.com ajax.aspnetcdn.com",
                FontSrc = $"{CSP.Self} fonts.googleapis.com",
                ImgSrc = CSP.Wildcard,
                StyleSrc = $"{CSP.Self} {CSP.UnsafeInline} fonts.googleapis.com",
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public override void OnResultExecuting(ResultExecutingContext context) {
            var result = context.Result;

            if (result is ViewResult) {
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Type-Options")) {
                    context.HttpContext.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                }
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Frame-Options")) {
                    context.HttpContext.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
                }

                // Once for standards compliant browsers.
                if (!context.HttpContext.Response.Headers.ContainsKey("Content-Security-Policy")) {
                    context.HttpContext.Response.Headers.Add("Content-Security-Policy", _csp.ToString());
                }

                // And once again for IE.
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Security-Policy")) {
                    context.HttpContext.Response.Headers.Add("X-Content-Security-Policy", _csp.ToString());
                }
            }
        }
    }

    /// <summary>
    /// https://content-security-policy.com/
    /// </summary>
    public class CSP : IEnumerable<string>
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>();

        /// <summary>
        /// Wildcard, allows any URL except data: blob: filesystem: schemes.
        /// </summary>
        public const string Wildcard = "*";

        /// <summary>
        /// Allows loading resources from the same origin (same scheme, host and port).
        /// </summary>
        public const string Self = "'self'";

        /// <summary>
        /// Prevents loading resources from any source.
        /// </summary>
        public const string None = "'none'";

        /// <summary>
        /// Allows loading resources via the data scheme (eg Base64 encoded images).
        /// </summary>
        public const string Data = "data:";

        /// <summary>
        /// Allows loading resources via the data scheme (eg Base64 encoded images).
        /// </summary>
        public const string UnsafeInline = "'unsafe-inline'";

        /// <summary>
        /// Content Security policy regarding all sources.
        /// </summary>
        public string DefaultSrc {
            get => GetValueOrDefult(nameof(DefaultSrc));
            set => SetValue(nameof(DefaultSrc), value);
        }

        /// <summary>
        /// Content Security policy regarding script sources.
        /// </summary>
        public string ScriptSrc {
            get => GetValueOrDefult(nameof(ScriptSrc));
            set => SetValue(nameof(ScriptSrc), value);
        }

        /// <summary>
        /// Content Security policy regarding cascading stylesheets.
        /// </summary>
        public string StyleSrc {
            get => GetValueOrDefult(nameof(StyleSrc));
            set => SetValue(nameof(StyleSrc), value);
        }

        /// <summary>
        /// Content Security policy regarding image sources.
        /// </summary>
        public string ImgSrc {
            get => GetValueOrDefult(nameof(ImgSrc));
            set => SetValue(nameof(ImgSrc), value);
        }

        /// <summary>
        /// Content Security policy regarding font sources.
        /// </summary>
        public string FontSrc {
            get => GetValueOrDefult(nameof(FontSrc));
            set => SetValue(nameof(FontSrc), value);
        }

        /// <summary>
        /// Endpoint to report back the csp report from server.
        /// </summary>
        public string ReportUri {
            get => GetValueOrDefult(nameof(ReportUri));
            set => SetValue(nameof(ReportUri), value);
        }

        /// <summary>
        /// Gets the <see cref="IEnumerator{T}"/> to iterate the underliing values for each policy part.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<string> GetEnumerator() => values.Where(x => x.Value != null).Select(x => $"{GetUrlCasing(x.Key)} {x.Value}").GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// The string representation of the CSP header value. Ready to use.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => string.Join("; ", this);

        private string GetValueOrDefult(string key) => values.ContainsKey(key) ? values[key] : null;

        private void SetValue(string key, string value) {
            if (values.ContainsKey(key)) {
                values[key] = value;
            } else {
                values.Add(key, value);
            }
        }

        private string GetUrlCasing(string pascalCasing) => Regex.Replace(pascalCasing, "([A-Z][a-z]+)", "-$1", RegexOptions.Compiled).Trim().ToLowerInvariant().TrimStart('-');

        /// <summary>
        /// Helper object that represents a browser CSP report request object.
        /// </summary>
        public class ReportRequest
        {
            /// <summary>
            /// The CSP report member
            /// </summary>
            [JsonProperty(PropertyName = "csp-report")]
            public Report CspReport { get; set; }
        }

        /// <summary>
        /// Helper object that represents a browser CSP report object.
        /// </summary>
        public class Report
        {
            /// <summary>
            /// Document uri that the error happened
            /// </summary>
            [JsonProperty(PropertyName = "document-uri")]
            public string DocumentUri { get; set; }

            /// <summary>
            /// The referrer
            /// </summary>
            [JsonProperty(PropertyName = "referrer")]
            public string Referrer { get; set; }

            /// <summary>
            /// Which directive was violated
            /// </summary>
            [JsonProperty(PropertyName = "violated-directive")]
            public string ViolatedDirective { get; set; }

            /// <summary>
            /// Effective directive
            /// </summary>
            [JsonProperty(PropertyName = "effective-directive")]
            public string EffectiveDirective { get; set; }

            /// <summary>
            /// Original policy
            /// </summary>
            [JsonProperty(PropertyName = "original-policy")]
            public string OriginalPolicy { get; set; }

            /// <summary>
            /// The resource uri that was blocked
            /// </summary>
            [JsonProperty(PropertyName = "blocked-uri")]
            public string BlockedUri { get; set; }

            /// <summary>
            /// Status code
            /// </summary>
            [JsonProperty(PropertyName = "status-code")]
            public int StatusCode { get; set; }
        }
    }
}
