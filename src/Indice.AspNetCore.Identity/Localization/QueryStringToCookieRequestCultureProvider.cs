using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Identity.Localization
{
    /// <summary>
    /// Searches for the specified query string parameter inside the URL sets a cookie for localization.
    /// </summary>
    public class QueryStringToCookieRequestCultureProvider : RequestCultureProvider
    {
        private readonly RequestLocalizationOptions _requestLocalizationOptions;
        /// <summary>
        /// The default name of the query string parameter to look for culture.
        /// </summary>
        public static readonly string DefaultParameterName = "culture";

        /// <summary>
        /// Creates a new instance of <see cref="QueryStringToCookieRequestCultureProvider"/>.
        /// </summary>
        /// <param name="requestLocalizationOptions">Specifies options for the <see cref="RequestLocalizationMiddleware"/>.</param>
        public QueryStringToCookieRequestCultureProvider(IOptions<RequestLocalizationOptions> requestLocalizationOptions) {
            _requestLocalizationOptions = requestLocalizationOptions?.Value ?? throw new ArgumentNullException(nameof(requestLocalizationOptions));
        }

        /// <summary>
        /// The name of the query string parameter to look for culture. Default is <see cref="DefaultParameterName"/>.
        /// </summary>
        public string QueryParameterName { get; set; } = DefaultParameterName;

        /// <inheritdoc />
        public override Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext) {
            if (httpContext == null) {
                throw new ArgumentNullException(nameof(httpContext));
            }
            var queryString = httpContext.Request.Query;
            var exists = queryString.TryGetValue(QueryParameterName, out var culture);
            if (!exists) {
                exists = queryString.TryGetValue("returnUrl", out var returnUrl);
                if (exists) {
                    var request = returnUrl.ToArray()[0];
                    var uri = new Uri("https://example.com" + request);
                    var returnUrlQueryString = QueryHelpers.ParseQuery(uri.Query);
                    var requestCulture = returnUrlQueryString.FirstOrDefault(x => x.Key == QueryParameterName).Value;
                    var cultureFromReturnUrl = requestCulture.ToString();
                    if (string.IsNullOrEmpty(cultureFromReturnUrl)) {
                        return NullProviderCultureResult;
                    }
                    culture = cultureFromReturnUrl;
                }
            }
            var providerResultCulture = ParseDefaultParameterValue(culture);
            var cultureCookieName = CookieRequestCultureProvider.DefaultCookieName;
            var cookieRequestCultureProvider = _requestLocalizationOptions.RequestCultureProviders.SingleOrDefault(provider => typeof(CookieRequestCultureProvider).IsAssignableFrom(provider.GetType()));
            if (cookieRequestCultureProvider != null) {
                cultureCookieName = ((CookieRequestCultureProvider)cookieRequestCultureProvider).CookieName;
            }
            if (!string.IsNullOrEmpty(culture.ToString())) {
                var cookie = httpContext.Request.Cookies[cultureCookieName];
                var newCookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
                if (string.IsNullOrEmpty(cookie) || cookie != newCookieValue) {
                    httpContext.Response.Cookies.Append(cultureCookieName, newCookieValue);
                }
            }
            return Task.FromResult(providerResultCulture);
        }

        private static ProviderCultureResult ParseDefaultParameterValue(string value) {
            if (string.IsNullOrWhiteSpace(value)) {
                return null;
            }
            var cultureName = value;
            var uiCultureName = value;
            if (cultureName == null && uiCultureName == null) {
                return null;
            }
            if (cultureName != null && uiCultureName == null) {
                uiCultureName = cultureName;
            }
            if (cultureName == null && uiCultureName != null) {
                cultureName = uiCultureName;
            }
            return new ProviderCultureResult(cultureName, uiCultureName);
        }
    }
}
