#if NET9_0_OR_GREATER
using Duende.IdentityModel;
#else
using IdentityModel;
#endif
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
        ArgumentNullException.ThrowIfNull(httpContext);
        var queryString = httpContext.Request.Query;
        if (httpContext.Request.Method == HttpMethod.Get.Method &&
            httpContext.Request.Path.StartsWithSegments("/connect/authorize", StringComparison.OrdinalIgnoreCase) &&
            queryString.TryGetValue(QueryParameterName, out var requestCulture)) {
            if (string.IsNullOrEmpty(requestCulture) || string.IsNullOrEmpty(requestCulture.ToString())) {
                return NullProviderCultureResult;
            }
            var identityCookieProvider = new IdentityCookieRequestCultureProvider();
            var providerResultCulture = ParseDefaultParameterValue(requestCulture.ToString());
            if (identityCookieProvider.SetLanguage(httpContext, providerResultCulture?.UICultures.FirstOrDefault().ToString())) {
                return Task.FromResult(providerResultCulture);
            }
        }
        return NullProviderCultureResult;
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
        return new ProviderCultureResult(value, value);
    }
}
