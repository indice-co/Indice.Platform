using Indice.AspNetCore.EmbeddedUI;

namespace Indice.Features.Cases.UI;

/// <summary>Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</summary>
public class CasesUIOptions : SpaUIOptions
{
    /// <summary>The case management api url.</summary>
    public string ApiUrl { get; set; }

    /// <summary> The html application language.</summary>
    public string Lang { get; set; }

    /// <summary>The absolute url to the i18n assets folder.</summary>
    /// <remarks>This folder contains translation files eg: el.json, en.json etc.</remarks>
    public string I18nAssets { get; set; }

    /// <summary>A list containing the tags of the canvases that should be displayed in the dashboard.</summary>
    /// <remarks>If left null or empty all canvases are displayed.</remarks>
    public List<string> DashboardTags { get; set; }

    /// <summary>Creates a new instance <see cref="CasesUIOptions"/>.</summary>
    public CasesUIOptions() {
        ConfigureIndexParameters = args => {
            args[$"%({nameof(ApiUrl)})"] = ApiUrl;
            args[$"%({nameof(Lang)})"] = Lang;
            args[$"%({nameof(I18nAssets)})"] = I18nAssets;
            args[$"%({nameof(DashboardTags)})"] = DashboardTags?.Count > 0 ? string.Join(',', DashboardTags) : null;
        };
    }
}
