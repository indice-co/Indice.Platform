namespace Indice.Services.Configuration;

/// <summary>
/// Webhook processing services options
/// </summary>
public sealed class WebhooksSettings
{
    /// <summary>The name is used to mark the section found inside a configuration file.</summary>
    public static readonly string Name = "Webhooks";

    /// <summary>Indicates whether webhook processing is enabled.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>The maximum number of webhook event batches that can be queued before producers experience backpressure.</summary>
    public int ChannelCapacity { get; set; } = 100;
}
