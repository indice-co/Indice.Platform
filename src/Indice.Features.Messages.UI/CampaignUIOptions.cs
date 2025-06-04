using Indice.AspNetCore.EmbeddedUI;

namespace Indice.Features.Messages.UI;

/// <summary>Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</summary>
public class CampaignUIOptions : SpaUIOptions
{
    /// <summary>Enables the Media Library UI feature.</summary>
    public bool EnableMediaLibrary { get; set; } = false;

    /// <summary>Creates a new instance <see cref="CampaignUIOptions"/>.</summary>
    public CampaignUIOptions() {
        ClientId = "messaging-ui";
        Scope = "offline_access messages media";
        DocumentTitle = "Messaging UI";
        ConfigureIndexParameters = (args) => {
            args.Add($"%({nameof(EnableMediaLibrary)})", EnableMediaLibrary.ToString());
        };
    }
}
