using Indice.AspNetCore.EmbeddedUI;

namespace Indice.Features.Messages.UI;

/// <summary>Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</summary>
public class CampaignUIOptions : SpaUIOptions
{
#if NET7_0_OR_GREATER
    /// <summary>Enables the Media Library UI feature.</summary>
    public bool EnableMediaLibrary { get; set; } = false;
#endif
    /// <summary>Enables a rich text editor for Campaign content. defaults to <strong>/api</strong> which is the same origin. </summary>
    /// <remarks>Can also be pointed to a an exteranl origin ei: messaging.indice.gr.</remarks>
    public string ApiBaseUrl { get; set; } = "/api";
    /// <summary>Creates a new instance <see cref="CampaignUIOptions"/>.</summary>
    public CampaignUIOptions() {
        ClientId = "messaging-ui";
        Scope = "offline_access messages media";
        DocumentTitle = "Messaging UI";
        ConfigureIndexParameters = (args) => {
#if NET7_0_OR_GREATER
            args.Add($"%({nameof(EnableMediaLibrary)})", EnableMediaLibrary.ToString());
#endif
            args.Add($"%({nameof(ApiBaseUrl)})", ApiBaseUrl);
        };
    }
}
