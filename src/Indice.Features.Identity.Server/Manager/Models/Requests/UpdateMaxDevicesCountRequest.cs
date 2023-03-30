namespace Indice.Features.Identity.Server.Manager.Models.Requests;

/// <summary>Models the request to update the max devices number for the user.</summary>
public class UpdateMaxDevicesCountRequest
{
    /// <summary>The number to apply for devices count.</summary>
    public int Count { get; set; }
}
