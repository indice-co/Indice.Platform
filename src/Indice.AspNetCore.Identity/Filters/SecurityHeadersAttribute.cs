using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace Indice.AspNetCore.Identity.Filters
{
    public class SecurityHeadersAttribute : ActionFilterAttribute
    {
        private readonly CSP _csp;

        public SecurityHeadersAttribute() : this(null) { }

        public SecurityHeadersAttribute(CSP scp) {
            _csp = scp ?? new CSP {
                DefaultSrc = $"{CSP.Self}",
                ScriptSrc = $"{CSP.Self} ajax.googleapis.com ajax.aspnetcdn.com",
                FontSrc = $"{CSP.Self} fonts.googleapis.com",
                ImgSrc = CSP.Wildcard
            };
        }

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

        public string DefaultSrc {
            get => GetValueOrDefult(nameof(DefaultSrc));
            set => SetValue(nameof(DefaultSrc), value);
        }

        public string ScriptSrc {
            get => GetValueOrDefult(nameof(ScriptSrc));
            set => SetValue(nameof(ScriptSrc), value);
        }

        public string StyleSrc {
            get => GetValueOrDefult(nameof(StyleSrc));
            set => SetValue(nameof(StyleSrc), value);
        }

        public string ImgSrc {
            get => GetValueOrDefult(nameof(ImgSrc));
            set => SetValue(nameof(ImgSrc), value);
        }

        public string FontSrc {
            get => GetValueOrDefult(nameof(FontSrc));
            set => SetValue(nameof(FontSrc), value);
        }

        public string ReportUri {
            get => GetValueOrDefult(nameof(ReportUri));
            set => SetValue(nameof(ReportUri), value);
        }

        public IEnumerator<string> GetEnumerator() => values.Where(x => x.Value != null).Select(x => $"{GetUrlCasing(x.Key)} {x.Value}").GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString() => string.Join("; ", this);

        private string GetValueOrDefult(string key) => values.ContainsKey(nameof(key)) ? values[nameof(key)] : null;

        private void SetValue(string key, string value) {
            if (values.ContainsKey(nameof(key))) {
                values[nameof(key)] = value;
            } else {
                values.Add(nameof(key), value);
            }
        }

        private string GetUrlCasing(string pascalCasing) => Regex.Replace(pascalCasing, "([A-Z][a-z]+)", "-$1", RegexOptions.Compiled).Trim().ToLowerInvariant();

        public class ReportRequest
        {
            [JsonProperty(PropertyName = "csp-report")]
            public Report CspReport { get; set; }
        }

        public class Report
        {
            [JsonProperty(PropertyName = "document-uri")]
            public string DocumentUri { get; set; }

            [JsonProperty(PropertyName = "referrer")]
            public string Referrer { get; set; }

            [JsonProperty(PropertyName = "violated-directive")]
            public string ViolatedDirective { get; set; }

            [JsonProperty(PropertyName = "effective-directive")]
            public string EffectiveDirective { get; set; }

            [JsonProperty(PropertyName = "original-policy")]
            public string OriginalPolicy { get; set; }

            [JsonProperty(PropertyName = "blocked-uri")]
            public string BlockedUri { get; set; }

            [JsonProperty(PropertyName = "status-code")]
            public int StatusCode { get; set; }
        }
    }
}
