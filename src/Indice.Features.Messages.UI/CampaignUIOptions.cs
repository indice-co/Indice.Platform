using Indice.AspNetCore.EmbeddedUI;

namespace Indice.Features.Messages.UI;

/// <summary>Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</summary>
public class CampaignUIOptions : SpaUIOptions
{
#if NET7_0_OR_GREATER
    /// <summary>Enables the Media Library UI feature.</summary>
    public bool EnableMediaLibrary { get; set; } = false;
    /// <summary>Enables a rich text editor for Campaign content.</summary>
    public bool EnableRichTextEditor { get; set; } = false;
#endif
    /// <summary>Creates a new instance <see cref="CampaignUIOptions"/>.</summary>
    public CampaignUIOptions() {
        ConfigureIndexParameters = (args) => {
#if NET7_0_OR_GREATER
            args.Add($"%({nameof(EnableMediaLibrary)})", EnableMediaLibrary.ToString());
            args.Add($"%({nameof(EnableRichTextEditor)})", EnableRichTextEditor.ToString());
#endif
        };
    }
}
