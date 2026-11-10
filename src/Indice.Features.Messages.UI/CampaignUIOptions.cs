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

    /// <summary>Enables the personal inbox (notifications bell and "my messages" page). Defaults to <i>true</i>.</summary>
    public bool EnableInbox { get; set; } = true;

    /// <summary>
    /// The url of the SignalR hub used for real time inbox notifications (i.e. <i>/hubs/messaging</i> or <i>https://api.example.com/hubs/messaging</i>).
    /// Relative urls are resolved against the root (origin) of the api address. When not set the inbox rarely polls for the unread count.
    /// </summary>
    /// <remarks>
    /// When using the SignalR proxy (<c>MapSignalRProxy()</c>) the hub must match the one configured through <c>UseInboxSignalRNotifications()</c>
    /// and be part of the <c>SignalRProxy:AllowedHubs</c>.
    /// </remarks>
    public string? InboxHubUrl { get; set; }

    /// <summary>Creates a new instance <see cref="CampaignUIOptions"/>.</summary>
    public CampaignUIOptions() {
        ClientId = "messaging-ui";
        Scope = "offline_access messages media";
        DocumentTitle = "Messaging UI";
        ConfigureIndexParameters = (args) => {
            args.Add($"%({nameof(EnableMediaLibrary)})", EnableMediaLibrary.ToString());
            args.Add($"%({nameof(MaxFileSize)})", MaxFileSize.ToString());
            args.Add($"%({nameof(AcceptableFileExtensions)})", AcceptableFileExtensions);
            args.Add($"%({nameof(EnableInbox)})", EnableInbox.ToString());
            args.Add($"%({nameof(InboxHubUrl)})", InboxHubUrl ?? string.Empty);
        };
    }
}
