using Indice.AspNetCore.EmbeddedUI;

namespace Indice.Features.Risk.UI;

/// <summary>Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</summary>
public class RisksUIOptions : SpaUIOptions
{
    /// <summary>The Risks BackOffice API URL.</summary>
    public string? ApiUrl { get; set; }

    /// <summary>Creates a new instance <see cref="RisksUIOptions"/>.</summary>
    public RisksUIOptions() {
        ClientId = "risks-app";
        Scope = "openid profile role email risk";
        DocumentTitle = "Risk UI";
        ConfigureIndexParameters = args => {
            args[$"%({nameof(ApiUrl)})"] = ApiUrl;
        };
    }
}
