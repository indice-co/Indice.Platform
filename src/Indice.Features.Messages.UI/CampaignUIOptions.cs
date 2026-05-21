using Indice.AspNetCore.EmbeddedUI;

namespace Indice.Features.Messages.UI;

/// <summary>Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</summary>
public class CampaignUIOptions : SpaUIOptions
{
    /// <summary>Enables the Media Library UI feature.</summary>
    public bool EnableMediaLibrary { get; set; } = false;

    /// <summary>The maximum acceptable size of the files to be uploaded. Defaults to <i>10MB</i>.</summary>
    public int MaxFileSize { get; set; } = 10 * 1024 * 1024;

    /// <summary>The acceptable file extensions. Defaults to <i>.png, .jpg, .gif, .webp</i>.</summary>
    public string AcceptableFileExtensions { get; set; } = ".png, .jpg, .gif, .webp";

    /// <summary>Creates a new instance <see cref="CampaignUIOptions"/>.</summary>
    public CampaignUIOptions() {
        ClientId = "messaging-ui";
        Scope = "offline_access messages media";
        DocumentTitle = "Messaging UI";
        ConfigureIndexParameters = (args) => {
            args.Add($"%({nameof(EnableMediaLibrary)})", EnableMediaLibrary.ToString());
            args.Add($"%({nameof(MaxFileSize)})", MaxFileSize.ToString());
            args.Add($"%({nameof(AcceptableFileExtensions)})", AcceptableFileExtensions);
        };
    }
}
