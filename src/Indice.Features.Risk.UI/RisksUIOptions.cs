using System.Globalization;
using Indice.AspNetCore.EmbeddedUI;

namespace Indice.Features.Risk.UI;

/// <summary>Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</summary>
public class RisksUIOptions : SpaUIOptions
{
    /// <summary>The Risks BackOffice API URL.</summary>
    public string? ApiUrl { get; set; }

    /// <summary>Gets or sets the two-letter ISO language code that identifies the language of the content.</summary>
    public string? Lang { get; set; }

    /// <summary>Creates a new instance <see cref="RisksUIOptions"/>.</summary>
    public RisksUIOptions() {
        ClientId = "risk-ui";
        Scope = "openid profile role email risk";
        DocumentTitle = "Risk UI";
        ConfigureIndexParameters = args => {
            args[$"%({nameof(ApiUrl)})"] = ApiUrl;
            args[$"%({nameof(Lang)})"] = Lang ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        };
    }
}
