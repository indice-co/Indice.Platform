using System.Net;
using Indice.AspNetCore.EmbeddedUI;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.AdminUI;

/// <summary>Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</summary>
public class AdminUIOptions : SpaUIOptions
{

    /// <summary> The html application language.</summary>
    public string? Lang { get; set; }

    /// <summary>The absolute url to the i18n assets folder.</summary>
    /// <remarks>This folder contains translation files eg: el.json, en.json etc.</remarks>
    public string? I18nAssets { get; set; }

    /// <summary>The url of the API documentation linked from the navigation menu. Defaults to <b>docs/index.html</b>.</summary>
    /// <remarks>
    /// Relative urls (e.g. <c>docs/index.html</c> or <c>/scalar</c>) are resolved against the <see cref="SpaUIOptions.Authority"/>.
    /// Absolute urls (e.g. <c>https://developers.example.com</c>) are used as is.
    /// </remarks>
    public string? ApiDocsUrl { get; set; } = "docs/index.html";

    /// <summary>Creates a new instance <see cref="AdminUIOptions"/>.</summary>
    public AdminUIOptions() {
        ClientId = "idsrv-admin-ui";
        Scope = "openid profile role email identity identity:clients identity:users identity:logs";
        DocumentTitle = "IAM UI";
        ConfigureIndexParameters = args => {
            args[$"%({nameof(Lang)})"] = Lang;
            args[$"%({nameof(I18nAssets)})"] = I18nAssets;
            args[$"%({nameof(ApiDocsUrl)})"] = WebUtility.HtmlEncode(ApiDocsUrl?.Trim());
        };
    }
}
