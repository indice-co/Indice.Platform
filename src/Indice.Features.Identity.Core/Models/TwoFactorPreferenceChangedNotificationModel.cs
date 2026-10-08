using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Models;

/// <summary>Represents the data model used for constructing an email notification when a user's two-factor authentication preference is changed.</summary>
public class TwoFactorPreferenceChangedNotificationModel
{
    /// <summary>The user instance.</summary>
    public UserEventContext User { get; set; } = null!;
    /// <summary>The username.</summary>
    public string? UserName => User?.UserName;
    /// <summary>User's name for display purposes.</summary>
    public string? DisplayName { get; set; }
    /// <summary>The two-factor authentication method code that was selected.</summary>
    public string AuthenticationMethodCode { get; set; } = null!;
    /// <summary>Gets or sets the timestamp indicating when the event occurred.</summary>
    public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>The email subject.</summary>
    public string? Subject { get; set; }
    /// <summary>The email description.</summary>
    public string? Description { get; set; }
}
