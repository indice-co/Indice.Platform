using System.ComponentModel.DataAnnotations;
using Indice.Services;

namespace Indice.Features.Identity.Server.Totp.Models;

/// <summary>Request object used by an authenticated user in order to get a new Time base one time access token via one of the supported MFA mechanisms.</summary>
public class TotpRequest
{
    /// <summary>Delivery channel.</summary>
    [Required]
    public TotpDeliveryChannel Channel { get; set; }
    /// <summary>Optionally pass the reason to generate the TOTP.</summary>
    public string? Purpose { get; set; }
    /// <summary>The message to be sent in the SMS/Viber or PushNotification. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</summary>
    [Required]
    public string Message { get; set; } = string.Empty;
    /// <summary>The payload data in JSON string to be sent in the Push Notification.</summary>
    public dynamic? Data { get; set; }
    /// <summary>The type of the Push Notification.</summary>
    /// <remarks>This applies only for <see cref="TotpDeliveryChannel.PushNotification"/> channel.</remarks>
    public string? Classification { get; set; }
    /// <summary>The subject of the message for the <see cref="TotpDeliveryChannel.PushNotification"/> <see cref="Channel"/>.</summary>
    public string? Subject { get; set; }
}
