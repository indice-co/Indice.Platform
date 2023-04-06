namespace Indice.Features.Identity.Server.Devices.Models;
/// <summary>Update an existing user device model.</summary>
public class UpdateDeviceRequest
{
    /// <summary>Device name.</summary>
    public string Name { get; set; }
    /// <summary>Tags are used to route notifications to the correct set of device handles.</summary>
    public List<string> Tags { get; set; }
    /// <summary>Platform Notification Service (PNS) obtained from client platform.</summary>
    public string PnsHandle { get; set; }
    /// <summary>Device model.</summary>
    public string Model { get; set; }
    /// <summary>Device OS version.</summary>
    public string OsVersion { get; set; }
    /// <summary>Extra metadata for the device.</summary>
    public dynamic Data { get; set; }
}