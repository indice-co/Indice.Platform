using Indice.AspNetCore.EmbeddedUI;

namespace Indice.Features.Agents.UI;

/// <summary>Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</summary>
public class AgentsUIOptions : SpaUIOptions
{
    private const string DefaultAgentName = "Dex";

    /// <summary> The html application language.</summary>
    public string? Lang { get; set; }

    /// <summary>The name of the visual theme to apply. Selects the colour palette as well as the logo and favicon. Built-in themes are <b>dex</b>, <b>kosmocar</b>, <b>sfakianakis</b> and <b>papadopoulos</b>. Defaults to <b>dex</b>.</summary>
    public string? Theme { get; set; }

    /// <summary>The display name of the assistant, shown wherever the UI names it (brand, greetings, input placeholder, browser tab). Defaults to <b>Dex</b>.</summary>
    public string? AssistantName { get; set; }

    /// <summary>Creates a new instance <see cref="AgentsUIOptions"/>.</summary>
    public AgentsUIOptions() {
        ClientId = "dex-ui";
        Scope = "openid profile role email chat";
        DocumentTitle = DefaultAgentName;
        ApiBase = "/api";
        ConfigureIndexParameters = args => {
            args[$"%({nameof(Lang)})"] = Lang ?? System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var assistantName = string.IsNullOrWhiteSpace(AssistantName) ? DefaultAgentName : AssistantName.Trim();
            args[$"%({nameof(AssistantName)})"] = System.Net.WebUtility.HtmlEncode(assistantName);
            // Unless the host set its own title, the browser tab follows the agent name.
            if (DocumentTitle == DefaultAgentName) {
                args[$"%({nameof(DocumentTitle)})"] = System.Net.WebUtility.HtmlEncode(assistantName);
            }
            args[$"%({nameof(Theme)})"] = string.IsNullOrWhiteSpace(Theme) ? "dex" : Theme.Trim().ToLowerInvariant();
        };
    }
}