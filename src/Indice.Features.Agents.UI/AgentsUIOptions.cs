using Indice.AspNetCore.EmbeddedUI;

namespace Indice.Features.Agents.UI;

/// <summary>Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</summary>
public class AgentsUIOptions : SpaUIOptions
{

    /// <summary> The html application language.</summary>
    public string? Lang { get; set; }


    /// <summary>Creates a new instance <see cref="AgentsUIOptions"/>.</summary>
    public AgentsUIOptions() {
        ClientId = "dex-ui";
        Scope = "openid profile role email chat";
        DocumentTitle = "Dex";
        ApiBase = "/api";
        ConfigureIndexParameters = args => {
            args[$"%({nameof(Lang)})"] = Lang;
        };
    }
}