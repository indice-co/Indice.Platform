using System.Globalization;

namespace Indice.Features.Identity.UI;

/// <summary>An extra link rendered in the site footer. See <see cref="IdentityUIOptions.FooterLinks"/>.</summary>
public class FooterLink
{
    /// <summary>Resolves the link text based on the current UI culture.</summary>
    public Func<CultureInfo, string> TextResolver { get; set; } = _ => string.Empty;
    /// <summary>The link URL. Ignored when <see cref="UrlResolver"/> is set.</summary>
    public string? Url { get; set; }
    /// <summary>Resolves the link URL based on the current UI culture. Takes precedence over <see cref="Url"/>.</summary>
    public Func<CultureInfo, string?>? UrlResolver { get; set; }
    /// <summary>The link target. Defaults to <c>_blank</c>.</summary>
    public string? Target { get; set; } = "_blank";
    /// <summary>The link rel attribute. Defaults to <c>noopener</c> when <see cref="Target"/> is <c>_blank</c>.</summary>
    public string? Rel { get; set; }

    /// <summary>Resolves the text for the given culture.</summary>
    public string GetText(CultureInfo culture) => TextResolver(culture);
    /// <summary>Resolves the URL for the given culture.</summary>
    public string? GetUrl(CultureInfo culture) => UrlResolver is not null ? UrlResolver(culture) : Url;
    /// <summary>Resolves the effective rel attribute.</summary>
    public string? GetRel() => Rel ?? ("_blank".Equals(Target, StringComparison.OrdinalIgnoreCase) ? "noopener" : null);

    /// <summary>Creates a new <see cref="FooterLink"/>.</summary>
    /// <param name="text">Resolves the link text based on the current UI culture.</param>
    /// <param name="url">The link URL.</param>
    /// <param name="target">The link target. Defaults to <c>_blank</c>.</param>
    public static FooterLink Create(Func<CultureInfo, string> text, string url, string? target = "_blank") =>
        new() { TextResolver = text, Url = url, Target = target };

    /// <summary>Creates a new <see cref="FooterLink"/> with a culture-specific URL.</summary>
    /// <param name="text">Resolves the link text based on the current UI culture.</param>
    /// <param name="url">Resolves the link URL based on the current UI culture.</param>
    /// <param name="target">The link target. Defaults to <c>_blank</c>.</param>
    public static FooterLink Create(Func<CultureInfo, string> text, Func<CultureInfo, string?> url, string? target = "_blank") =>
        new() { TextResolver = text, UrlResolver = url, Target = target };
}
