using Indice.Features.GeoIP;
using Indice.Features.Identity.Core.Events.Models;
using Indice.Features.Identity.Server.Manager.Models;

namespace Indice.Features.Identity.Core.Models;
/// <summary>
/// Represents the data model used for constructing an email notification when a user's password is changed.
/// </summary>
/// <remarks>This model contains information about the user, the device that initiated the password change (if
/// applicable), and additional details such as the email subject and display name.</remarks>
public class SecurityNotificationModel : IEmailModel
{
    /// <summary>The user instance.</summary>
    public UserEventContext User { get; set; } = null!;
    /// <summary>The device metadata.</summary>
    public DeviceEventContext Device { get; set; } = null!;
    /// <summary>The device that initiated the password change, if any.</summary>
    public UserDeviceEventContext? UserDevice { get; set; }
    /// <summary>The client that initiated the password change, if any.</summary>
    public ClientEventContext? Client { get; set; }
    /// <summary>The user's email address.</summary>
    public IPAddressLocation Location { get; set; } = null!;
    /// <summary>Gets or sets the timestamp indicating when the event occurred.</summary>
    public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>The username</summary>
    public string? UserName => User?.UserName;
    /// <summary>The recipients email</summary>
    public string? RecipientEmail => User?.Email;
    /// <summary>User's name for display purposes.</summary>
    public string? DisplayName { get; set; }
    /// <summary>The email subject.</summary>
    public string? Subject { get; set; }
    /// <summary>The email description.</summary>
    public string? Description { get; set; }
}
