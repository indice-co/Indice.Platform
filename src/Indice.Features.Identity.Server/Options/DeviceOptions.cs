using Indice.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.Server.Options;

/// <summary>Options used to configure <b>Devices</b> feature.</summary>
public class DeviceOptions
{
    internal IServiceCollection Services { get; set; } = null!;
    /// <summary>The default <see cref="TotpDeliveryChannel"/> used when a device operation requires TOTP protection. Defaults to <see cref="TotpDeliveryChannel.Sms"/>.</summary>
    public TotpDeliveryChannel DefaultTotpDeliveryChannel { get; set; } = TotpDeliveryChannel.Sms;
}
