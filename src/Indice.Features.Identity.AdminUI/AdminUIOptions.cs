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

    /// <summary>Creates a new instance <see cref="AdminUIOptions"/>.</summary>
    public AdminUIOptions() {
        ClientId = "idsrv-admin-ui";
        Scope = "openid profile role email identity identity:clients identity:users identity:logs";
        DocumentTitle = "IAM UI";
        ConfigureIndexParameters = args => {
            args[$"%({nameof(Lang)})"] = Lang;
            //args[$"%({nameof(I18nAssets)})"] = I18nAssets;
        };
    }
}
