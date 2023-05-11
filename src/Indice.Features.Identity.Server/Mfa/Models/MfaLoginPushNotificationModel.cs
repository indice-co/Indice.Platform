namespace Indice.Features.Identity.Server.Mfa.Models;

/// <summary>Request model for sending push notification for MFA login.</summary>
public class MfaLoginPushNotificationModel
{
    /// <summary>Connection identifier for SignalR.</summary>
    public string? ConnectionId { get; set; }
}
