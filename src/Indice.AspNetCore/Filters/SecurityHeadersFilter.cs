using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Indice.AspNetCore.Filters
{
    /// <summary>
    /// Sets the Content Security policy header for the current action.
    /// </summary>
    public sealed class SecurityHeadersAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// This key is used to communicate generated nonces between the tag helper and the filter.
        /// </summary>
        public const string CSP_SCRIPT_NONCE_HTTPCONTEXT_KEY = "CSP_SCRIPT_NONCE";
        /// <summary>
        /// This key is used to communicate generated nonces between the tag helper and the filter.
        /// </summary>
        public const string CSP_STYLE_NONCE_HTTPCONTEXT_KEY = "CSP_STYLE_NONCE";

        /// <summary>
        /// Constructor defaults to allowing self origin, plus Google for fonts and scripts (Google cdn) and wildcard for images.
        /// </summary>
        public SecurityHeadersAttribute() { }

        /// <summary>
        /// Code that executes before an action is executed.
        /// </summary>
        /// <param name="context">A context for result filters, specifically <see cref="IResultFilter.OnResultExecuting(ResultExecutingContext)"/></param>
        public override void OnResultExecuting(ResultExecutingContext context) {
            var result = context.Result;
            var requestPolicy = (CSP)context.HttpContext.RequestServices.GetService(typeof(CSP));
            if (result is ViewResult) {
                context.HttpContext.Response.OnStarting(() => {
                    var policy = (requestPolicy ?? CSP.DefaultPolicy).Clone();
                    if (context.HttpContext.Items.ContainsKey(CSP_SCRIPT_NONCE_HTTPCONTEXT_KEY)) {
                        var nonceList = (List<string>)context.HttpContext.Items[CSP_SCRIPT_NONCE_HTTPCONTEXT_KEY];
                        foreach (var nonce in nonceList) {
                            policy.AddScriptSrc($"'nonce-{nonce}'");
                        }
                    }
                    if (context.HttpContext.Items.ContainsKey(CSP_STYLE_NONCE_HTTPCONTEXT_KEY)) {
                        var nonceList = (List<string>)context.HttpContext.Items[CSP_STYLE_NONCE_HTTPCONTEXT_KEY];
                        foreach (var nonce in nonceList) {
                            policy.AddStyleSrc($"'nonce-{nonce}'");
                        }
                    }
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
                    return Task.CompletedTask;
                });
                
            }
        }
    }

    /// <summary>
    /// Content Security Policy https://content-security-policy.com/
    /// </summary>
    public class CSP : IEnumerable<string>, ICloneable
    {
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        /// <summary>
        /// Create a new nonce. Essentially creates an 16 byte length array (128 bit) and converts to base64 string.
        /// </summary>
        /// <returns></returns>
        public static string CreateNonce(int length = 128) {
            var bytes = new byte[length / 8];
            Rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
        /// <summary>
        /// Default policy.
        /// </summary>
        public static readonly CSP DefaultPolicy = new CSP {
            DefaultSrc = $"{Self}",
            ObjectSrc = $"{None}",
            BaseUri = $"{Self}",
            FrameAncestors = $"{None}",
            Sandbox = $"allow-forms allow-same-origin allow-scripts",
            ScriptSrc = $"{Self}",
            FontSrc = $"{Self} fonts.googleapis.com fonts.gstatic.com",
            ImgSrc = $"{Wildcard} {Data}",
            StyleSrc = $"{Self} {UnsafeInline} fonts.googleapis.com"
        };

        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        /// <summary>
        /// Wildcard, allows any URL except data: blob: filesystem: schemes.
        /// </summary>
        public const string Wildcard = "*";
        /// <summary>
        /// Allows loading resources from the same origin (same scheme, host and port).
        /// </summary>
        public const string Self = "'self'";
        /// <summary>
        /// Prevents loading resources from any source. Refers to the empty set; that is, no URLs match. The single quotes are required.
        /// </summary>
        public const string None = "'none'";
        /// <summary>
        /// Allows loading resources via the data scheme (eg. Base64 encoded images).
        /// </summary>
        public const string Data = "data:";
        /// <summary>
        /// Allows the use of eval() and similar methods for creating code from strings. You must include the single quotes.
        /// </summary>
        public const string UnsafeEval = "'unsafe-eval'";
        /// <summary>
        /// Allows the use of inline resources, such as inline &lt;script&gt; elements, javascript: URLs, inline event handlers, and inline &lt;style&gt; elements. The single quotes are required.
        /// </summary>
        public const string UnsafeInline = "'unsafe-inline'";
        /// <summary>
        /// Allows enabling specific inline event handlers. If you only need to allow inline event handlers and not inline &lt;script&gt; elements or javascript: URLs, this is a safer method than using the unsafe-inline expression.
        /// </summary>
        public const string UnsafeHashes = "'unsafe-hashes'";
        /// <summary>
        /// The strict-dynamic source expression specifies that the trust explicitly given to a script present in the markup, by accompanying it with a nonce or a hash, 
        /// shall be propagated to all the scripts loaded by that root script. 
        /// At the same time, any whitelist or source expressions such as 'self' or 'unsafe-inline' will be ignored. See script-src for an example.
        /// </summary>
        public const string StrictDynamic = "'strict-dynamic'";


        /// <summary>
        /// [CSP Level 1]
        /// The default-src directive defines the default policy for fetching resources such as 
        /// JavaScript, Images, CSS, Fonts, AJAX requests, Frames, HTML5 Media. 
        /// Not all directives fallback to default-src. See the Source List Reference for possible values.
        /// </summary>
        public string DefaultSrc {
            get => GetValueOrDefault(nameof(DefaultSrc));
            set => SetValue(nameof(DefaultSrc), value);
        }
        /// <summary>
        /// [CSP Level 1]
        /// Defines valid sources of JavaScript.
        /// </summary>
        public string ScriptSrc {
            get => GetValueOrDefault(nameof(ScriptSrc));
            set => SetValue(nameof(ScriptSrc), value);
        }
        /// <summary>
        /// [CSP Level 1]
        ///Defines valid sources of stylesheets or CSS.
        /// </summary>
        public string StyleSrc {
            get => GetValueOrDefault(nameof(StyleSrc));
            set => SetValue(nameof(StyleSrc), value);
        }
        /// <summary>
        /// [CSP Level 1]
        /// Defines valid sources of images.
        /// </summary>
        public string ImgSrc {
            get => GetValueOrDefault(nameof(ImgSrc));
            set => SetValue(nameof(ImgSrc), value);
        }

        /// <summary>
        /// [CSP Level 1]
        /// Applies to XMLHttpRequest (AJAX), WebSocket, fetch(), &lt;a ping&gt; or EventSource. If not allowed the browser emulates a 400 HTTP status code.
        /// </summary>
        public string ConnectSrc {
            get => GetValueOrDefault(nameof(ConnectSrc));
            set => SetValue(nameof(ConnectSrc), value);
        }

        /// <summary>
        /// [CSP Level 1]
        /// Content Security Policy regarding font sources.
        /// </summary>
        public string FontSrc {
            get => GetValueOrDefault(nameof(FontSrc));
            set => SetValue(nameof(FontSrc), value);
        }

        /// <summary>
        /// [CSP Level 1]
        /// Specifies valid sources for the &lt;object&gt;, &lt;embed&gt;, and &lt;applet&gt; elements.
        /// </summary>
        public string ObjectSrc {
            get => GetValueOrDefault(nameof(ObjectSrc));
            set => SetValue(nameof(ObjectSrc), value);
        }

        /// <summary>
        /// [CSP Level 1]
        /// Defines valid sources of audio and video, eg HTML5 &lt;audio&gt;, &lt;video&gt; elements.
        /// </summary>
        public string MediaSrc {
            get => GetValueOrDefault(nameof(MediaSrc));
            set => SetValue(nameof(MediaSrc), value);
        }

        /// <summary>
        /// [CSP Level 1]
        /// Defines valid sources for loading frames. In CSP Level 2 frame-src was deprecated in favor of the child-src directive. 
        /// CSP Level 3, has undeprecated frame-src and it will continue to defer to child-src if not present.
        /// </summary>
        public string FrameSrc {
            get => GetValueOrDefault(nameof(FrameSrc));
            set => SetValue(nameof(FrameSrc), value);
        }

        /// <summary>
        /// [CSP Level 1]
        /// Enables a sandbox for the requested resource similar to the iframe sandbox attribute. 
        /// The sandbox applies a same origin policy, prevents popups, plugins and script execution is blocked. 
        /// You can keep the sandbox value empty to keep all restrictions in place, or add values: 
        /// allow-forms allow-same-origin allow-scripts allow-popups, allow-modals, allow-orientation-lock, 
        /// allow-pointer-lock, allow-presentation, allow-popups-to-escape-sandbox, and allow-top-navigation
        /// </summary>
        public string Sandbox {
            get => GetValueOrDefault(nameof(Sandbox));
            set => SetValue(nameof(Sandbox), value);
        }

        /// <summary>
        /// [CSP Level 1]
        /// Instructs the browser to POST a reports of policy failures to this URI. 
        /// You can also use Content-Security-Policy-Report-Only as the HTTP header name to instruct the browser to only send reports (does not block anything). 
        /// This directive is deprecated in CSP Level 3 in favor of the report-to directive.
        /// </summary>
        public string ReportUri {
            get => GetValueOrDefault(nameof(ReportUri));
            set => SetValue(nameof(ReportUri), value);
        }

        /// <summary>
        /// [CSP Level 2]
        /// Defines valid sources for web workers and nested browsing contexts loaded using elements such as &lt;frame&gt; and &lt;iframe&gt;
        /// </summary>
        public string ChildSrc {
            get => GetValueOrDefault(nameof(ChildSrc));
            set => SetValue(nameof(ChildSrc), value);
        }

        /// <summary>
        /// [CSP Level 2]
        /// Defines valid sources that can be used as an HTML &lt;form&gt; action.
        /// </summary>
        public string FormAction {
            get => GetValueOrDefault(nameof(FormAction));
            set => SetValue(nameof(FormAction), value);
        }

        /// <summary>
        /// [CSP Level 2]
        /// Defines valid sources for embedding the resource using &lt;frame&gt; &lt;iframe&gt; &lt;object&gt; &lt;embed&gt; &lt;applet&gt;. 
        /// Setting this directive to 'none' should be roughly equivalent to X-Frame-Options: DENY
        /// </summary>
        public string FrameAncestors {
            get => GetValueOrDefault(nameof(FrameAncestors));
            set => SetValue(nameof(FrameAncestors), value);
        }

        /// <summary>
        /// [CSP Level 2]
        /// Defines valid MIME types for plugins invoked via &lt;object&gt; and &lt;embed&gt;. 
        /// To load an &lt;applet&gt; you must specify application/x-java-applet.
        /// </summary>
        public string PluginTypes {
            get => GetValueOrDefault(nameof(PluginTypes));
            set => SetValue(nameof(PluginTypes), value);
        }

        /// <summary>
        /// [CSP Level 2]
        /// Defines a set of allowed URLs which can be used in the src attribute of a HTML base tag.
        /// </summary>
        public string BaseUri {
            get => GetValueOrDefault(nameof(BaseUri));
            set => SetValue(nameof(BaseUri), value);
        }

        /// <summary>
        /// [CSP Level 3]
        /// Defines a reporting group name defined by a Report-To HTTP response header. See the Reporting API for more info.
        /// </summary>
        public string ReportTo {
            get => GetValueOrDefault(nameof(ReportTo));
            set => SetValue(nameof(ReportTo), value);
        }

        /// <summary>
        /// [CSP Level 3]
        /// Restricts the URLs which may be loaded as a Worker, SharedWorker or ServiceWorker.
        /// </summary>
        public string WorkerSrc {
            get => GetValueOrDefault(nameof(WorkerSrc));
            set => SetValue(nameof(WorkerSrc), value);
        }

        /// <summary>
        /// [CSP Level 3]
        /// Restricts the URLs that application manifests can be loaded.
        /// </summary>
        public string ManifestSrc {
            get => GetValueOrDefault(nameof(ManifestSrc));
            set => SetValue(nameof(ManifestSrc), value);
        }

        /// <summary>
        /// [CSP Level 3]
        /// Defines valid sources for request prefetch and prerendering, for example via the link tag with rel="prefetch" or rel="prerender":
        /// </summary>
        public string PrefetchSrc {
            get => GetValueOrDefault(nameof(PrefetchSrc));
            set => SetValue(nameof(PrefetchSrc), value);
        }

        /// <summary>
        /// [CSP Level 3]
        /// Restricts the URLs that the document may navigate to by any means. For example when a link is clicked, 
        /// a form is submitted, or window.location is invoked. 
        /// If form-action is present then this directive is ignored for form submissions.
        /// </summary>
        public string NavigateTo {
            get => GetValueOrDefault(nameof(NavigateTo));
            set => SetValue(nameof(NavigateTo), value);
        }

        /// <summary>
        /// Appends an entry to the current CSP policy at the specified key (ie script-src etc.).
        /// </summary>
        /// <param name="key">The key to ammend.</param>
        /// <param name="value">The value to add.</param>
        /// <returns>Returns the current instance of the Content Security Policy.</returns>
        public CSP Add(string key, string value) {
            if (key.Contains('-')) {
                // this is url casing.
                key = GetPascalCasing(key);
            }
            if (_values.ContainsKey(key)) {
                if (_values[key] == null) {
                    _values[key] = value;
                } else if (!_values[key].Contains(value)) {
                    _values[key] += $" {value}";
                    // If the directive contains 'none' but a value is allowed then we have to remove the 'none' value.
                    // To do: Maybe we should consider removing all existing value if 'none' is added.
                    _values[key] = _values[key].Replace($"{None} ", string.Empty);
                } 
            } else {
                _values.Add(key, value);
            }
            return this;
        }

        /** level1 **/

        /// <summary>
        /// [CSP Level 1]
        /// The default-src directive defines the default policy for fetching resources such as 
        /// JavaScript, Images, CSS, Fonts, AJAX requests, Frames, HTML5 Media. 
        /// Not all directives fallback to default-src. See the Source List Reference for possible values.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddDefaultSrc(string value) => Add(nameof(DefaultSrc), value);
        /// <summary>
        /// [CSP Level 1]
        /// Defines valid sources of JavaScript.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddScriptSrc(string value) => Add(nameof(ScriptSrc), value);
        /// <summary>
        /// [CSP Level 1]
        ///Defines valid sources of stylesheets or CSS.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddStyleSrc(string value) => Add(nameof(StyleSrc), value);
        /// <summary>
        /// [CSP Level 1]
        /// Defines valid sources of images.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddImgSrc(string value) => Add(nameof(ImgSrc), value);
        /// <summary>
        /// [CSP Level 1]
        /// Applies to XMLHttpRequest (AJAX), WebSocket, fetch(), &lt;a ping&gt; or EventSource. If not allowed the browser emulates a 400 HTTP status code.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddConnectSrc(string value) => Add(nameof(ConnectSrc), value);
        /// <summary>
        /// [CSP Level 1]
        /// Content Security Policy regarding font sources.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddFontSrc(string value) => Add(nameof(FontSrc), value);
        /// <summary>
        /// [CSP Level 1]
        /// Specifies valid sources for the &lt;object&gt;, &lt;embed&gt;, and &lt;applet&gt; elements.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddObjectSrc(string value) => Add(nameof(ObjectSrc), value);
        /// <summary>
        /// [CSP Level 1]
        /// Defines valid sources of audio and video, eg HTML5 &lt;audio&gt;, &lt;video&gt; elements.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddMediaSrc(string value) => Add(nameof(MediaSrc), value);
        /// <summary>
        /// [CSP Level 1]
        /// Defines valid sources for loading frames. In CSP Level 2 frame-src was deprecated in favor of the child-src directive. 
        /// CSP Level 3, has undeprecated frame-src and it will continue to defer to child-src if not present.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddFrameSrc(string value) => Add(nameof(FrameSrc), value);
        /// <summary>
        /// [CSP Level 1]
        /// Enables a sandbox for the requested resource similar to the iframe sandbox attribute. 
        /// The sandbox applies a same origin policy, prevents popups, plugins and script execution is blocked. 
        /// You can keep the sandbox value empty to keep all restrictions in place, or add values: 
        /// allow-forms allow-same-origin allow-scripts allow-popups, allow-modals, allow-orientation-lock, 
        /// allow-pointer-lock, allow-presentation, allow-popups-to-escape-sandbox, and allow-top-navigation
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddSandbox(string value) => Add(nameof(Sandbox), value);
        /// <summary>
        /// [CSP Level 1]
        /// Instructs the browser to POST a reports of policy failures to this URI. 
        /// You can also use Content-Security-Policy-Report-Only as the HTTP header name to instruct the browser to only send reports (does not block anything). 
        /// This directive is deprecated in CSP Level 3 in favor of the report-to directive.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddReportUri(string value) => Add(nameof(ReportUri), value);

        /** level2 **/
        /// <summary>
        /// [CSP Level 2]
        /// Defines valid sources for web workers and nested browsing contexts loaded using elements such as &lt;frame&gt; and &lt;iframe&gt;
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddChildSrc(string value) => Add(nameof(ChildSrc), value);
        /// <summary>
        /// [CSP Level 2]
        /// Defines valid sources that can be used as an HTML &lt;form&gt; action.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddFormAction(string value) => Add(nameof(FormAction), value);
        /// <summary>
        /// [CSP Level 2]
        /// Defines valid sources for embedding the resource using &lt;frame&gt; &lt;iframe&gt; &lt;object&gt; &lt;embed&gt; &lt;applet&gt;. 
        /// Setting this directive to 'none' should be roughly equivalent to X-Frame-Options: DENY
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddFrameAncestors(string value) => Add(nameof(FrameAncestors), value);
        /// <summary>
        /// [CSP Level 2]
        /// Defines valid MIME types for plugins invoked via &lt;object&gt; and &lt;embed&gt;. 
        /// To load an &lt;applet&gt; you must specify application/x-java-applet.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddPluginTypes(string value) => Add(nameof(PluginTypes), value);
        /// <summary>
        /// [CSP Level 2]
        /// Defines a set of allowed URLs which can be used in the src attribute of a HTML base tag.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddBaseUri(string value) => Add(nameof(BaseUri), value);

        /** level3 **/

        /// <summary>
        /// [CSP Level 3]
        /// Defines a reporting group name defined by a Report-To HTTP response header. See the Reporting API for more info.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddReportTo(string value) => Add(nameof(ReportTo), value);
        /// <summary>
        /// [CSP Level 3]
        /// Restricts the URLs which may be loaded as a Worker, SharedWorker or ServiceWorker.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddWorkerSrc(string value) => Add(nameof(WorkerSrc), value);
        /// <summary>
        /// [CSP Level 3]
        /// Restricts the URLs that application manifests can be loaded.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddManifestSrc(string value) => Add(nameof(ManifestSrc), value);
        /// <summary>
        /// [CSP Level 3]
        /// Defines valid sources for request prefetch and prerendering, for example via the link tag with rel="prefetch" or rel="prerender":
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddPrefetchSrc(string value) => Add(nameof(PrefetchSrc), value);
        /// <summary>
        /// [CSP Level 3]
        /// Restricts the URLs that the document may navigate to by any means. For example when a link is clicked, 
        /// a form is submitted, or window.location is invoked. 
        /// If form-action is present then this directive is ignored for form submissions.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public CSP AddNavigateTo(string value) => Add(nameof(NavigateTo), value);

        /// <summary>
        /// Gets the <see cref="IEnumerator{T}"/> to iterate the underliing values for each policy part.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<string> GetEnumerator() => _values.Where(x => x.Value != null).Select(x => $"{GetUrlCasing(x.Key)} {x.Value}").GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// The string representation of the CSP header value. Ready to use.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => string.Join("; ", this);

        private string GetValueOrDefault(string key) => _values.ContainsKey(key) ? _values[key] : null;

        private void SetValue(string key, string value) {
            if (_values.ContainsKey(key)) {
                _values[key] = value;
            } else {
                _values.Add(key, value);
            }
        }

        private string GetUrlCasing(string pascalCasing) => Regex.Replace(pascalCasing, "([A-Z][a-z]+)", "-$1", RegexOptions.Compiled).Trim().ToLowerInvariant().TrimStart('-');
        private string GetPascalCasing(string urlCasing) => string.Join("", urlCasing.ToLowerInvariant().Split('-').Select(x => char.ToUpperInvariant(x[0]) + x.Substring(1)));

        /// <summary>
        /// Clone the CSP into a new instance.
        /// </summary>
        /// <returns></returns>
        public CSP Clone() {
            return new CSP {
                // level1
                DefaultSrc = DefaultSrc,
                ScriptSrc = ScriptSrc,
                StyleSrc = StyleSrc,
                ImgSrc = ImgSrc,
                ConnectSrc = ConnectSrc,
                FontSrc = FontSrc,
                ObjectSrc = ObjectSrc,
                MediaSrc = MediaSrc,
                FrameSrc = FrameSrc,
                Sandbox = Sandbox,
                ReportUri = ReportUri,
                // level2
                ChildSrc = ChildSrc,
                FormAction = FormAction,
                FrameAncestors = FrameAncestors,
                PluginTypes = PluginTypes,
                BaseUri = BaseUri,
                // level3
                ReportTo = ReportTo,
                WorkerSrc = WorkerSrc,
                ManifestSrc = ManifestSrc,
                PrefetchSrc = PrefetchSrc,
                NavigateTo = NavigateTo
            };
        }

        object ICloneable.Clone() => Clone();

        /// <summary>
        /// Helper object that represents a browser CSP report request object.
        /// </summary>
        public class ReportRequest
        {
            /// <summary>
            /// The CSP report member.
            /// </summary>
            [JsonPropertyName("csp-report")]
            public Report CspReport { get; set; }
        }

        /// <summary>
        /// Helper object that represents a browser CSP report object.
        /// </summary>
        public class Report
        {
            /// <summary>
            /// Document uri that the error happened.
            /// </summary>
            [JsonPropertyName("document-uri")]
            public string DocumentUri { get; set; }

            /// <summary>
            /// The referrer.
            /// </summary>
            [JsonPropertyName("referrer")]
            public string Referrer { get; set; }

            /// <summary>
            /// Which directive was violated.
            /// </summary>
            [JsonPropertyName("violated-directive")]
            public string ViolatedDirective { get; set; }

            /// <summary>
            /// Effective directive.
            /// </summary>
            [JsonPropertyName("effective-directive")]
            public string EffectiveDirective { get; set; }

            /// <summary>
            /// Original policy.
            /// </summary>
            [JsonPropertyName("original-policy")]
            public string OriginalPolicy { get; set; }

            /// <summary>
            /// The resource uri that was blocked.
            /// </summary>
            [JsonPropertyName("blocked-uri")]
            public string BlockedUri { get; set; }

            /// <summary>
            /// Status code.
            /// </summary>
            [JsonPropertyName("status-code")]
            public int StatusCode { get; set; }
        }
    }
}
