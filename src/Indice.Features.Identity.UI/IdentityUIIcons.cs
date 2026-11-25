using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.UI;

/// <summary>Iconography of the Bootstrap5 variant of the end-user Identity UI.</summary>
/// <remarks>
/// Every icon is a <see href="https://lucide.dev/icons">Lucide</see> icon served from a curated SVG sprite (<see cref="SpritePath"/>) and
/// rendered with the <c>&lt;icon name="…" /&gt;</c> tag helper. The sprite is generated at build time and only contains the icons referenced
/// by the built-in pages (literal <c>&lt;icon name="…"&gt;</c> usages and the names returned by this class).
/// </remarks>
public static class IdentityUIIcons
{
    /// <summary>The application relative path of the SVG sprite that contains the icons.</summary>
    public const string SpritePath = "~/lib/lucide/sprite.svg";

    /// <summary>The icon used when an authentication method has no dedicated icon.</summary>
    public const string DefaultAuthenticationMethod = "message-square-text";

    /// <summary>Gets the Lucide icon name that represents an authentication method (MFA channel).</summary>
    /// <param name="code">The authentication method code (i.e. <c>sms</c>, <c>email</c>, <c>authenticatorapp</c>).</param>
    /// <param name="fallback">The icon to use when the code is unknown.</param>
    public static string ForAuthenticationMethod(string? code, string fallback = DefaultAuthenticationMethod) => code?.ToLowerInvariant() switch {
        // Lucide ships no brand icons, so Viber is represented by a generic chat bubble.
        "viber" => "message-circle",
        "sms" => "message-square-text",
        "email" => "mail",
        "authenticatorapp" => "shield-check",
        "trusteddevice" => "smartphone",
        "fido2" => "fingerprint",
        "recoverycode" => "rectangle-ellipsis",
        _ => fallback
    };

    /// <summary>Resolves the versioned URL of the icon sprite, for client side rendering of icons (<c>&lt;use href="{sprite}#{name}"&gt;</c>).</summary>
    /// <param name="viewContext">The current view context.</param>
    public static string GetSpriteHref(ViewContext viewContext) {
        ArgumentNullException.ThrowIfNull(viewContext);
        var services = viewContext.HttpContext.RequestServices;
        var url = services.GetRequiredService<IUrlHelperFactory>().GetUrlHelper(viewContext).Content(SpritePath);
        var fileVersionProvider = services.GetService<IFileVersionProvider>();
        return fileVersionProvider?.AddFileVersionToPath(viewContext.HttpContext.Request.PathBase, url) ?? url;
    }
}
