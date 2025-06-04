using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.Core.Localization;
/// <summary>
/// A custom implementation of <see cref="CookieRequestCultureProvider"/> that uses a different cookie name for storing the user's preferred culture information.
/// </summary>
public class IdentityCookieRequestCultureProvider : CookieRequestCultureProvider
{
    /// <summary>
    /// Represent the default cookie name used to track the user's preferred culture information, which is ".AspNetCore.Culture".
    /// </summary>
    public static new readonly string DefaultCookieName = "idsrv.culture";

    /// <inheritdoc/>
    public IdentityCookieRequestCultureProvider() {
        CookieName = DefaultCookieName;
    }

    /// <summary>
    /// Sets the language preference for the current user by updating the culture cookie.
    /// </summary>
    /// <remarks>This method updates the culture cookie in the HTTP response to reflect the specified or
    /// default  language preference. The cookie is essential for applying the new culture and persists for one
    /// year.</remarks>
    /// <param name="httpContext">The <see cref="HttpContext"/> representing the current HTTP request and response.</param>
    /// <param name="culture">The two-letter ISO language name representing the desired culture. If the value is null, empty, or  not
    /// supported, the default culture specified in the application's localization options will be used.</param>
    public bool SetLanguage(HttpContext httpContext, string? culture) {
        ArgumentNullException.ThrowIfNull(httpContext);
        if (Options == null) {
            Options = httpContext.RequestServices.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
        }
        HashSet<string> supportedCultures = [.. (Options?.SupportedCultures ?? []).Select(x => x.TwoLetterISOLanguageName)];
        if (string.IsNullOrWhiteSpace(culture) || !supportedCultures.Contains(culture)) {
            culture = Options?.DefaultRequestCulture.Culture.TwoLetterISOLanguageName;
        }
        if (string.IsNullOrWhiteSpace(culture)) {
            return false; // No culture specified, do not set the cookie.
        }
        httpContext.Response.Cookies.Append(
            CookieName,
            MakeCookieValue(new RequestCulture(culture!)), new CookieOptions {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true, // Critical setting to apply new culture.
                Path = "/",
                HttpOnly = false
            }
        );
        return true;
    }
}
