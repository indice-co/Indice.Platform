namespace Indice.Features.Identity.UI;

/// <summary>Names of the themes that ship with the Bootstrap5 variant of the end-user Identity UI.</summary>
/// <remarks>
/// The selected theme (<see cref="IdentityUIOptions.Theme"/>) is emitted as <c>data-theme="…"</c> on the <c>&lt;html&gt;</c> element.
/// The markup is identical for every theme: a theme is nothing more than a set of <c>--idui-*</c> CSS custom property values scoped to
/// <c>[data-theme="name"]</c> (see <c>wwwroot/css/themes/</c>). Hosts add a theme by picking any other name and shipping a stylesheet
/// that declares those properties for it.
/// </remarks>
public static class IdentityUIThemes
{
    /// <summary>A single centered column over a light page. Login and register never show the hero.</summary>
    public const string Minimal = "minimal";
    /// <summary>A full-viewport hero image; the login form floats on the right, register uses an inset image column. The default.</summary>
    public const string Split = "split";
    /// <summary>A fixed image column on the left; the form and the footer sit in the right column. Applies to every anonymous page.</summary>
    public const string Columns = "columns";
    /// <summary>A centered two-column floating panel (image left, form right) on a solid brand-coloured page.</summary>
    public const string Panel = "panel";
    /// <summary>All built-in theme names.</summary>
    public static readonly IReadOnlyList<string> All = [Minimal, Split, Columns, Panel];
}
