using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Options for configuring 'Device Authentication' feature.</summary>
public class DeviceAuthenticationOptions
{
    internal IServiceCollection Services { get; set; }
    internal IConfiguration Configuration { get; set; }
    /// <summary>Determines whether an OTP code is always sent as part of initiation and completion process of biometric login.</summary>
    public bool AlwaysSendOtp { get; set; }
}
