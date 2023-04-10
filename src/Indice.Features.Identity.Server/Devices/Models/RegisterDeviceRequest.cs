using System.ComponentModel.DataAnnotations;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Types;

namespace Indice.Features.Identity.Server.Devices.Models;
/// <summary>Register a device for push notifications.</summary>
public class RegisterDeviceRequest
{
    /// <summary>The device id to register for push notifications.</summary>
    [Required]
    public string DeviceId { get; set; } = string.Empty;
    /// <summary>Platform Notification Service (PNS) obtained from client platform.</summary>
    public string? PnsHandle { get; set; }
    /// <summary>Device name.</summary>
    public string? Name { get; set; }
    /// <summary>Client device platform.</summary>
    [Required]
    public DevicePlatform Platform { get; set; }
    /// <summary>Tags are used to route notifications to the correct set of device handles.</summary>
    public List<string>? Tags { get; set; }
    /// <summary>Device model.</summary>
    public string? Model { get; set; }
    /// <summary>Device OS version.</summary>
    public string? OsVersion { get; set; }
    /// <summary>Describes the type of a user device.</summary>
    public DeviceClientType? ClientType { get; set; }
    /// <summary>Extra metadata for the device.</summary>
    public dynamic? Data { get; set; }
}
