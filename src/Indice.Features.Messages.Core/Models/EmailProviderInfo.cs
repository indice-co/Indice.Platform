namespace Indice.Features.Messages.Core.Models;

/// <summary>Includes information about the default email provider.</summary>
public class EmailProviderInfo
{
    /// <summary>Initializes a new instance of <see cref="EmailProviderInfo"/></summary>
    public EmailProviderInfo(string sender, string displayName) {
        Sender = sender;
        DisplayName = displayName;
    }

    /// <summary>The default email sender.</summary>
    public string Sender { get; set; }
    /// <summary>The default sender's display name.</summary>
    public string DisplayName { get; set; }
}