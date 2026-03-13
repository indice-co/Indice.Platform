using Indice.Types;

namespace Indice.Features.ActivityLogs.Models;

/// <summary>Models a user agent (browser) type.</summary>
public class ActivityLogEntryDevice
{
    /// <summary>The device model.</summary>
    public string? Model { get; set; }
    /// <summary>The device platform.</summary>
    public DevicePlatform Platform { get; set; }
    /// <summary>The raw value of the 'UserAgent' header.</summary>
    public string UserAgent { get; set; } = null!;
    /// <summary>Browser display name.</summary>
    public string DisplayName { get; set; } = null!;
    /// <summary>The operating system name.</summary>
    public string? Os { get; set; }
    /// <summary>The httpclient used to serve this request. Can be a browser app or a custom native application</summary>
    public string UserAgentFamily { get; set; } = "Unknown";
}
