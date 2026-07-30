using HtmlAgilityPack;
using System.Linq;

namespace Indice.Features.Agents.Server.Services;

/// <summary>
/// Provides helper methods for retrieving favicons from web pages.
/// </summary>
public static class FaviconHelper
{
    /// <summary>
    /// Retrieves the favicon URL for a given page URL. It first attempts to fetch the page and look for any &lt;link&gt; elements that specify an icon. 
    /// If none are found, it falls back to the default /favicon.ico path.
    /// </summary>
    /// <param name="httpClient">The HttpClient instance used to fetch the page.</param>
    /// <param name="pageUrl">The URL of the page for which to retrieve the favicon.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The URL of the favicon.</returns>
    public static async Task<string?> GetFaviconUrlAsync(this HttpClient httpClient, string pageUrl, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageUrl);
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var pageUri)) {
            return null;
        }
        var baseUri = new Uri(pageUri.GetLeftPart(UriPartial.Authority));
        using var response = await httpClient.GetAsync(baseUri, cancellationToken);
        if (!response.IsSuccessStatusCode) {
            return new Uri(baseUri, "/favicon.ico").ToString();
        }
        // Load the page
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(await response.Content.ReadAsStringAsync(cancellationToken));

        // Look for any <link> that has "icon" in the rel attribute
        var iconNodes = doc.DocumentNode
            .SelectNodes("//link[contains(translate(@rel, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'icon')]");

        if (iconNodes != null) {
            // Prefer the first one that has an href
            var href = iconNodes
                .Select(node => node.GetAttributeValue("href", string.Empty))
                .FirstOrDefault(href => !string.IsNullOrWhiteSpace(href));
            if (!string.IsNullOrWhiteSpace(href)) {
                // Convert relative → absolute
                return MakeAbsoluteUrl(baseUri, href);
            }
        }

        return null;
    }

    private static string MakeAbsoluteUrl(Uri baseUri, string href) {
        if (Uri.TryCreate(href, UriKind.Absolute, out var absolute))
            return absolute.ToString();

        return new Uri(baseUri, href).ToString();
    }
}
