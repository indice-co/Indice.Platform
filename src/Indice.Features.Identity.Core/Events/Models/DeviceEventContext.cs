using Indice.Types;

namespace Indice.Features.Identity.Core.Events.Models;

/// <summary>Models a user agent (browser) type.</summary>
public class DeviceEventContext
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

    /// <summary>
    /// Creates a new instance of <see cref="DeviceEventContext"/> from a user agent string. 
    /// </summary>
    /// <param name="userAgent">The user agent string.</param>
    /// <returns>A new instance of <see cref="DeviceEventContext"/>.</returns>
    public static DeviceEventContext FromUserAgent(string? userAgent) {
        userAgent = string.IsNullOrWhiteSpace(userAgent) ?  "Unknown" : userAgent;
        var parser = new Indice.AspNetCore.UserAgent(userAgent);
        return new DeviceEventContext {
            UserAgent = userAgent,
            DisplayName = parser.DisplayName,
            Os = parser.Os,
            Platform = parser.DevicePlatform,
            UserAgentFamily = parser.UserAgentFamily
        };
    }
}
