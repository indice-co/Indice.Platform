using IdentityModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.WebUtilities;

namespace Indice.Features.Identity.Core.Mvc.Localization;

/// <summary>Searches for the specified query string parameter inside the URL sets a cookie for localization.</summary>
public class QueryStringToCookieRequestCultureProvider : RequestCultureProvider
{
    /// <summary>The default name of the query string parameter to look for culture.</summary>
    public static readonly string DefaultParameterName = "culture";

    /// <summary>The name of the query string parameter to look for culture. Default is <see cref="DefaultParameterName"/>.</summary>
    public string QueryParameterName { get; set; } = DefaultParameterName;

    /// <inheritdoc />
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext) {
        if (httpContext == null) {
            throw new ArgumentNullException(nameof(httpContext));
        }
        var queryString = httpContext.Request.Query;
        var exists = queryString.TryGetValue(QueryParameterName, out var culture);
        if (!exists) {
            exists = queryString.TryGetValue("returnUrl", out var returnUrl);
            if (exists) {
                var request = returnUrl.ToArray()[0];
#if NET6_0_OR_GREATER
                if (!Uri.TryCreate("https://example.com" + request.TrimStart('~'), new UriCreationOptions { }, out var uri)) {
                    return NullProviderCultureResult;
                }
#else
                if (!Uri.TryCreate("https://example.com" + request.TrimStart('~'), UriKind.Absolute, out var uri)) {
                    return NullProviderCultureResult;
                }
#endif
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
        if (!string.IsNullOrEmpty(culture.ToString())) {
            var cookie = httpContext.Request.Cookies[CookieRequestCultureProvider.DefaultCookieName];
            var newCookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
            if (string.IsNullOrEmpty(cookie) || cookie != newCookieValue) {
                httpContext.Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName, newCookieValue);
            }
        }
        return Task.FromResult(providerResultCulture);
    }

    /// <summary>A factory method used for creating a new instance of <see cref="QueryStringToCookieRequestCultureProvider"/> with a specified culture parameter to look for.</summary>
    /// <param name="queryParameterName">The name of the query string parameter to look for culture.</param>
    public static QueryStringToCookieRequestCultureProvider Create(string queryParameterName) => new() { QueryParameterName = queryParameterName };

    /// <summary>Creates a new instance of <see cref="QueryStringToCookieRequestCultureProvider"/> used to look for standard 'ui_locales' parameter in the authorize endpoint.</summary>
    public static QueryStringToCookieRequestCultureProvider CreateForUiLocales() => new() {
        QueryParameterName = OidcConstants.AuthorizeRequest.UiLocales
    };

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
