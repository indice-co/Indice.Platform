using Indice.Services;

namespace Indice.Features.Identity.Server.Totp.Models;

/// <summary>Request object used by an authenticated user in order to get a new Time base one time access token via one of the supported MFA mechanisms.</summary>
public class TotpRequest
{
    /// <summary>Delivery channel.</summary>
    public TotpDeliveryChannel? Channel { get; set; }
    /// <summary>Optionally pass the reason to generate the TOTP.</summary>
    public string? Purpose { get; set; }
    /// <summary>The message to be sent in the SMS/Viber or PushNotification. It's important for the message to contain the {0} placeholder in the position where the OTP should be placed.</summary>
    public string? Message { get; set; }
    /// <summary>The payload data in JSON string to be sent in the Push Notification.</summary>
    public dynamic? Data { get; set; }
    /// <summary>The type of the Push Notification.</summary>
    /// <remarks>This applies only for <see cref="TotpDeliveryChannel.PushNotification"/> channel.</remarks>
    public string? Classification { get; set; }
    /// <summary>The subject of the message for the <see cref="TotpDeliveryChannel.PushNotification"/> <see cref="Channel"/>.</summary>
    public string? Subject { get; set; }
    /// <summary>The user authentication method to be used.</summary>
    public string? AuthenticationMethod { get; set; }
    /// <summary>The email template to be used when <see cref="Channel"/> is <see cref="TotpDeliveryChannel.Email"/> or when <see cref="AuthenticationMethod"/> has a relevant channel.</summary>
    public string? EmailTemplate { get; set; }
}
