using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Indice.AspNetCore.Filters
{
    /// <summary>
    /// Sets the Content Security policy header for the current action.
    /// </summary>
    public class SecurityHeadersAttribute : ActionFilterAttribute
    {

        /// <summary>
        /// Constractor Defaults to allowing self origin plus google for fonts and scripts (google cdn) and wildcard for images.
        /// </summary>
        public SecurityHeadersAttribute() {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public override void OnResultExecuting(ResultExecutingContext context) {
            var result = context.Result;
            var requestPolicy = (CSP)context.HttpContext.RequestServices.GetService(typeof(CSP));
            var policy = requestPolicy ?? CSP.DefaultPolicy;
            if (result is ViewResult) {
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Type-Options")) {
                    context.HttpContext.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                }
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Frame-Options")) {
                    context.HttpContext.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
                }

                // Once for standards compliant browsers.
                if (!context.HttpContext.Response.Headers.ContainsKey("Content-Security-Policy")) {
                    context.HttpContext.Response.Headers.Add("Content-Security-Policy", policy.ToString());
                }

                // And once again for IE.
                if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Security-Policy")) {
                    context.HttpContext.Response.Headers.Add("X-Content-Security-Policy", policy.ToString());
                }
                // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Referrer-Policy
                var referrer_policy = "no-referrer";
                if (!context.HttpContext.Response.Headers.ContainsKey("Referrer-Policy")) {
                    context.HttpContext.Response.Headers.Add("Referrer-Policy", referrer_policy);
                }
            }
        }
    }

    /// <summary>
    /// Content Security Policy https://content-security-policy.com/
    /// </summary>
    public class CSP : IEnumerable<string>, ICloneable
    {
        /// <summary>
        /// Default policy
        /// </summary>
        public static readonly CSP DefaultPolicy = new CSP {
            DefaultSrc = $"{Self}",
            ObjectSrc = $"{None}",
            BaseUri = $"{Self}",
            FrameAncestors = $"{None}",
            Sandbox = $"allow-forms allow-same-origin allow-scripts",
            ScriptSrc = $"{Self} ajax.googleapis.com ajax.aspnetcdn.com stackpath.bootstrapcdn.com",
            FontSrc = $"{Self} fonts.googleapis.com",
            ImgSrc = $"{Wildcard} {Data}",
            StyleSrc = $"{Self} {UnsafeInline} fonts.googleapis.com stackpath.bootstrapcdn.com use.fontawesome.com",
        };

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
            get => GetValueOrDefault(nameof(DefaultSrc));
            set => SetValue(nameof(DefaultSrc), value);
        }

        /// <summary>
        /// Content Security policy regarding script sources.
        /// </summary>
        public string ScriptSrc {
            get => GetValueOrDefault(nameof(ScriptSrc));
            set => SetValue(nameof(ScriptSrc), value);
        }

        /// <summary>
        /// Content Security policy regarding cascading stylesheets.
        /// </summary>
        public string StyleSrc {
            get => GetValueOrDefault(nameof(StyleSrc));
            set => SetValue(nameof(StyleSrc), value);
        }

        /// <summary>
        /// Content Security policy regarding image sources.
        /// </summary>
        public string ImgSrc {
            get => GetValueOrDefault(nameof(ImgSrc));
            set => SetValue(nameof(ImgSrc), value);
        }

        /// <summary>
        /// Content Security policy regarding font sources.
        /// </summary>
        public string FontSrc {
            get => GetValueOrDefault(nameof(FontSrc));
            set => SetValue(nameof(FontSrc), value);
        }

        /// <summary>
        /// Specifies valid sources for the &lt;object&gt;, &lt;embed&gt;, and &lt;applet&gt; elements.
        /// </summary>
        public string ObjectSrc {
            get => GetValueOrDefault(nameof(ObjectSrc));
            set => SetValue(nameof(ObjectSrc), value);
        }

        /// <summary>
        /// connect-src directive restricts the URLs which can be loaded using script interfaces. The APIs that are restricted are:
        /// ping,
        /// Fetch,
        /// XMLHttpRequest,
        /// WebSocket, and
        /// EventSource.
        /// </summary>
        public string ConnectSrc {
            get => GetValueOrDefault(nameof(ConnectSrc));
            set => SetValue(nameof(ConnectSrc), value);
        }

        /// <summary>
        /// Content Security policy regarding frame ancestors. When the current page will be included in an iframe.
        /// </summary>
        public string FrameAncestors {
            get => GetValueOrDefault(nameof(FrameAncestors));
            set => SetValue(nameof(FrameAncestors), value);
        }

        /// <summary>
        /// Endpoint to report back the csp report from server.
        /// </summary>
        public string ReportUri {
            get => GetValueOrDefault(nameof(ReportUri));
            set => SetValue(nameof(ReportUri), value);
        }

        /// <summary>
        /// Restricts the URLs which can be used in a document's &lt;base&gt; element.
        /// </summary>
        public string BaseUri {
            get => GetValueOrDefault(nameof(BaseUri));
            set => SetValue(nameof(BaseUri), value);
        }

        /// <summary>
        /// Enables a sandbox for the requested resource similar to the &lt;iframe&gt; sandbox attribute.
        /// </summary>
        public string Sandbox {
            get => GetValueOrDefault(nameof(Sandbox));
            set => SetValue(nameof(Sandbox), value);
        }

        /// <summary>
        /// Appends an entry to the current CSP policy at the specified key (ie script-src etc.)
        /// </summary>
        /// <param name="key">The key to ammend</param>
        /// <param name="value">The value to add</param>
        /// <returns></returns>
        public CSP Add(string key, string value) {
            if (key.Contains('-')) {
                // this is url casing.
                key = GetPascalCasing(key);
            }
            if (values.ContainsKey(key)) {
                values[key] += $" {value}";
                // If the directive contains 'none' but a value is allowed then we have to remove the 'none' value.
                // To do: Maybe we should consider removing all existing value if 'none' is added.
                values[key] = values[key].Replace($"{None} ", string.Empty);
            } else {
                values.Add(key, value);
            }
            return this;
        }

        /// <summary>
        /// Appends an entry to the current CSP policy
        /// </summary>
        /// <param name="value">The value to add</param>
        /// <returns></returns>
        public CSP AddScriptSrc(string value) => Add(nameof(ScriptSrc), value);

        /// <summary>
        /// Appends an entry to the current CSP policy
        /// </summary>
        /// <param name="value">The value to add</param>
        /// <returns></returns>
        public CSP AddConnectSrc(string value) => Add(nameof(ConnectSrc), value);

        /// <summary>
        /// Appends an entry to the current CSP policy
        /// </summary>
        /// <param name="value">The value to add</param>
        /// <returns></returns>
        public CSP AddSandbox(string value) => Add(nameof(Sandbox), value);

        /// <summary>
        /// Appends an entry to the current CSP policy.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddFrameAncestors(string value) => Add(nameof(FrameAncestors), value);

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

        private string GetValueOrDefault(string key) => values.ContainsKey(key) ? values[key] : null;

        private void SetValue(string key, string value) {
            if (values.ContainsKey(key)) {
                values[key] = value;
            } else {
                values.Add(key, value);
            }
        }

        private string GetUrlCasing(string pascalCasing) => Regex.Replace(pascalCasing, "([A-Z][a-z]+)", "-$1", RegexOptions.Compiled).Trim().ToLowerInvariant().TrimStart('-');
        private string GetPascalCasing(string urlCasing) => string.Join("", urlCasing.ToLowerInvariant().Split('-').Select(x => char.ToUpperInvariant(x[0]) + x.Substring(1)));

        /// <summary>
        /// Clone the CSP into a new instance
        /// </summary>
        /// <returns></returns>
        public CSP Clone() {
            return new CSP {
                DefaultSrc = DefaultSrc,
                ObjectSrc = ObjectSrc,
                BaseUri = BaseUri,
                FrameAncestors = FrameAncestors,
                Sandbox = Sandbox,
                ScriptSrc = ScriptSrc,
                FontSrc = FontSrc,
                ImgSrc = ImgSrc,
                StyleSrc = StyleSrc,
                ConnectSrc = ConnectSrc
            };
        }

        object ICloneable.Clone() => Clone();

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
