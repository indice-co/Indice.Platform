using IdentityModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace Indice.Features.Identity.Core.Localization;

/// <summary>Searches for the specified query string parameter inside the URL sets a cookie for localization.</summary>
public class UiLocalesRequestCultureProvider : RequestCultureProvider
{
    /// <summary>The default name of the query string parameter to look for culture.</summary>
    /// <remarks>defaults to the Oidc standard parameter <strong>ui_locales</strong></remarks>
    public static readonly string DefaultParameterName = OidcConstants.AuthorizeRequest.UiLocales;

    /// <summary>The name of the query string parameter to look for culture. Default is <see cref="DefaultParameterName"/>.</summary>
    public string QueryParameterName { get; set; } = DefaultParameterName;

    /// <inheritdoc />
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext) {
        if (httpContext == null) {
            throw new ArgumentNullException(nameof(httpContext));
        }
        var queryString = httpContext.Request.Query;
        ProviderCultureResult? providerResultCulture = null;
        if (httpContext.Request.Method == HttpMethod.Get.Method &&
            httpContext.Request.Path.StartsWithSegments("/connect/authorize", StringComparison.OrdinalIgnoreCase) &&
            queryString.TryGetValue(QueryParameterName, out var requestCulture)) {
            if (string.IsNullOrEmpty(requestCulture)) {
                return NullProviderCultureResult;
            }
            providerResultCulture = ParseDefaultParameterValue(requestCulture);
            if (!string.IsNullOrEmpty(requestCulture.ToString())) {
                var cookie = httpContext.Request.Cookies[IdentityCookieRequestCultureProvider.DefaultCookieName];
                var newCookieValue = IdentityCookieRequestCultureProvider.MakeCookieValue(new RequestCulture(requestCulture!));
                if (string.IsNullOrEmpty(cookie) || cookie != newCookieValue) {
                    httpContext.Response.Cookies.Append(IdentityCookieRequestCultureProvider.DefaultCookieName, newCookieValue);
                }
            }
        }
        return Task.FromResult(providerResultCulture);
    }

    /// <summary>A factory method used for creating a new instance of <see cref="UiLocalesRequestCultureProvider"/> with a specified culture parameter to look for.</summary>
    /// <param name="queryParameterName">The name of the query string parameter to look for culture.</param>
    public static UiLocalesRequestCultureProvider Create(string queryParameterName) => new() { QueryParameterName = queryParameterName };

    /// <summary>Creates a new instance of <see cref="UiLocalesRequestCultureProvider"/> used to look for standard 'ui_locales' parameter in the authorize endpoint.</summary>
    public static UiLocalesRequestCultureProvider CreateForUiLocales() => new() {
        QueryParameterName = OidcConstants.AuthorizeRequest.UiLocales
    };

    private static ProviderCultureResult? ParseDefaultParameterValue(string? value) {
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
