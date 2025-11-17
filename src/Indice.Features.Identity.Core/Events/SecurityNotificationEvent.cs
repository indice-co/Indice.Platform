using Indice.Events;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Events.Models;
using Indice.Features.GeoIP;

namespace Indice.Features.Identity.SignInLogs.Events;
/// <summary>
/// Represents the data model used for constructing an email notification when a user's password is changed.
/// </summary>
/// <remarks>This model contains information about the user, the device that initiated the password change (if
/// applicable), and additional details such as the email subject and display name.</remarks>
public class SecurityNotificationEvent : IPlatformEvent
{
    /// <summary>Initializes a new instance of the <see cref="SecurityNotificationEvent"/> class.</summary>
    public SecurityNotificationEvent(string activity, UserEventContext user, IPAddressLocation location, string subject) {
        Activity = activity ?? throw new ArgumentNullException(nameof(activity));
        User = user ?? throw new ArgumentNullException(nameof(user));
        Location = location ?? throw new ArgumentNullException(nameof(user));
        Subject = subject ?? throw new ArgumentNullException(nameof(subject));
    }
    /// <summary>The activty that triggered the security notification.</summary>
    public string Activity { get; set; } = null!;
    /// <summary>The user instance.</summary>
    public UserEventContext User { get; set; } = null!;
    /// <summary>The user's email address.</summary>
    public IPAddressLocation Location { get; set; } = null!;
    /// <summary>The device that initiated the password change, if any.</summary>
    public UserDeviceEventContext? Device { get; set; }
    /// <summary>The client that initiated the password change, if any.</summary>
    public ClientEventContext? Client { get; set; }
    /// <summary>Gets or sets the timestamp indicating when the event occurred.</summary>
    public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>The translated Subject of the message.</summary>
    public string Subject { get; set; }
    /// <summary>The translated Body of the message.</summary>
    public string? Description { get; set; } = null;
}