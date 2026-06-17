using Indice.AspNetCore.EmbeddedUI;

namespace Indice.Features.Agents.UI;

/// <summary>Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</summary>
public class AgentsUIOptions : SpaUIOptions
{

    /// <summary> The html application language.</summary>
    public string? Lang { get; set; }


    /// <summary>Creates a new instance <see cref="AgentsUIOptions"/>.</summary>
    public AgentsUIOptions() {
        ClientId = "agents-ui";
        Scope = "openid profile role email agents";
        DocumentTitle = "Agents UI";
        ConfigureIndexParameters = args => {
            args[$"%({nameof(Lang)})"] = Lang;
        };
    }
}