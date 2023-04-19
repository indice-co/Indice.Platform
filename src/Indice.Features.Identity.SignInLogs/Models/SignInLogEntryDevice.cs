using Indice.Types;

namespace Indice.Features.Identity.SignInLogs.Models;

/// <summary>Models a user agent (browser) type.</summary>
public class SignInLogEntryDevice
{
    /// <summary>The device model.</summary>
    public string Model { get; set; }
    /// <summary>The device platform.</summary>
    public DevicePlatform Platform { get; set; }
    /// <summary>The raw value of the 'UserAgent' header.</summary>
    public string UserAgent { get; set; }
    /// <summary>Browser display name.</summary>
    public string DisplayName { get; set; }
    /// <summary>The operating system name.</summary>
    public string Os { get; set; }
}
