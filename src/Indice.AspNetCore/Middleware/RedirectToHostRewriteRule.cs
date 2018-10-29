using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Net.Http.Headers;

namespace Indice.AspNetCore.Middleware
{
    /// <summary>
    /// A Url rewrite rule to always redirect multiple bindings (azure defaults and other) to the one primary domain http binding.
    /// </summary>
    public class RedirectToHostRewriteRule : IRule
    {
        /// <summary>
        /// The host binding under which we are serving. This should invlude if any the port and scheme.
        /// </summary>
        /// <param name="hostDomain"></param>
        public RedirectToHostRewriteRule(string hostDomain = null) {
            if (!string.IsNullOrEmpty(hostDomain)) {
                Host = new HostString(hostDomain.TrimEnd('/'));
            }
        }

        /// <summary>
        /// The kind of redirect (Permanent or not)
        /// </summary>
        public int StatusCode { get; } = (int)HttpStatusCode.MovedPermanently;

        /// <summary>
        /// Dont run under localhost. (Defaults to true).
        /// </summary>
        public bool ExcludeLocalhost { get; set; } = true;

        /// <summary>
        /// The host binding under which we are serving. This should invlude if any the port and scheme.
        /// </summary>
        public HostString Host { get; }

        /// <summary>
        /// Applies the rule. Implementations of ApplyRule should set the value for <see cref="RewriteContext.Result"/>
        ///     (defaults to <seealso cref="RuleResult.ContinueRules"/>)
        /// </summary>
        /// <param name="context"></param>
        public void ApplyRule(RewriteContext context) {
            var request = context.HttpContext.Request;
            var requestHost = request.Host; // Does not include scheme.
            var requestHostWithScheme = $"{request.Scheme}://{requestHost.Host}{(requestHost.Port.HasValue ? $":{requestHost.Port}" : string.Empty)}";

            // If we work locally, then also continue execution.
            if (ExcludeLocalhost && string.Equals(requestHost.Host, "localhost", StringComparison.OrdinalIgnoreCase)) {
                context.Result = RuleResult.ContinueRules;
                return;
            }

            if (Host != null) {
                // If the current request host is the same as our setting, then continue execution.
                if (requestHostWithScheme.Equals(Host.Value, StringComparison.OrdinalIgnoreCase)) {
                    context.Result = RuleResult.SkipRemainingRules;
                    return;
                }
            }

            var newHost = Host != null ? Host : new HostString(requestHost.Value.Replace("www", string.Empty));
            var newPath = $"{newHost}{request.PathBase}{request.Path}{request.QueryString}";
            var response = context.HttpContext.Response;
            response.StatusCode = StatusCode;
            response.Headers[HeaderNames.Location] = newPath;
            context.Result = RuleResult.EndResponse;
        }
    }
}
