using System;
using System.Collections.Generic;
using Indice.AspNetCore.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Indice.AspNetCore.TagHelpers
{
    /// <summary>
    /// Suppresses the output of the element if the supplied predicate equates to <c>false</c>.
    /// </summary>
    [HtmlTargetElement("*", Attributes = "csp-nonce")]
    public class NonceTagHelper : TagHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// creates the tag helper
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        public NonceTagHelper(IHttpContextAccessor httpContextAccessor) {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// The predicate expression to test.
        /// </summary>
        [HtmlAttributeName("csp-nonce")]
        public bool Enabled { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="output"></param>
        public override void Process(TagHelperContext context, TagHelperOutput output) {
            if (Enabled) {
                var nonce = CSP.CreateNonce();
                var httpContext = _httpContextAccessor.HttpContext;
                List<string> nonceList;
                var key = string.Empty;
                if (string.Equals(context.TagName, "script", StringComparison.OrdinalIgnoreCase)) {
                    key = CSP.CSP_SCRIPT_NONCE_HTTPCONTEXT_KEY;
                } else if (string.Equals(context.TagName, "style", StringComparison.OrdinalIgnoreCase)) {
                    key = CSP.CSP_STYLE_NONCE_HTTPCONTEXT_KEY;
                }
                if (httpContext.Items.ContainsKey(key)) {
                    nonceList = (List<string>)httpContext.Items[key];
                } else {
                    nonceList = new List<string>();
                    httpContext.Items.Add(key, nonceList);
                }
                if (!output.Attributes.ContainsName("nonce")) {
                    output.Attributes.Add("nonce", nonce);
                    nonceList.Add(nonce);
                }
            }
        }

    }
}
