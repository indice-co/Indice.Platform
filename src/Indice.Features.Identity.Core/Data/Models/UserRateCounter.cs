namespace Indice.Features.Identity.Core.Data.Models;

/// <summary>Represents a purpose-scoped action attempt counter for a user.</summary>
public class UserRateCounter
{
    /// <summary>The user id this counter belongs to.</summary>
    public string UserId { get; set; } = null!;
    /// <summary>A free-form action name (for example, Sms:ChangePhoneNumber).</summary>
    public string ActionName { get; set; } = null!;
    /// <summary>The number of attempts recorded in the current active window.</summary>
    public int Count { get; set; }
    /// <summary>The UTC date until which the current window is active.</summary>
    public DateTimeOffset ResetDate { get; set; }
    /// <summary>The UTC date the latest attempt was recorded.</summary>
    public DateTimeOffset LastUpdate { get; set; }
}