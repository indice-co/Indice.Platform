namespace Indice.AspNetCore.Identity.Api.Models;

/// <summary>Trust device parameters payload.</summary>
public class TrustDeviceRequest
{
    /// <summary>The id of the device to remove before trusting the defined device.</summary>
    public string SwapDeviceId { get; set; }
}
