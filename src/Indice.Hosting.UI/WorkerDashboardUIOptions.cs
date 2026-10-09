using Indice.AspNetCore.EmbeddedUI;

namespace Indice.Hosting.UI;

/// <summary>Options for the workers dashboard single page application.</summary>
public class WorkerDashboardUIOptions : SpaUIOptions
{
    private const string DefaultTheme = "indice";

    /// <summary>The html application language.</summary>
    public string? Lang { get; set; }

    /// <summary>The daisyUI theme name. Defaults to <b>indice</b>.</summary>
    public string? Theme { get; set; }

    /// <summary>Creates a new instance of <see cref="WorkerDashboardUIOptions"/>.</summary>
    public WorkerDashboardUIOptions() {
        ClientId = "workers-ui";
        Scope = "openid profile role email workers";
        DocumentTitle = "Workers";
        ApiBase = "/api/workers";
        PathPrefix = "/workers";
        ConfigureIndexParameters = args => {
            args[$"%({nameof(Lang)})"] = Lang ?? System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var theme = string.IsNullOrWhiteSpace(Theme) ? DefaultTheme : Theme.Trim().ToLowerInvariant();
            theme = System.Text.RegularExpressions.Regex.Replace(theme, "[^a-z0-9_-]", string.Empty);
            args[$"%({nameof(Theme)})"] = string.IsNullOrEmpty(theme) ? DefaultTheme : theme;
        };
    }
}
