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
    /// <summary>Determines whether the current window is expired and a reset is due.</summary>
    /// <param name="now">The current date and time.</param>
    /// <returns>True if a reset is due; otherwise, false.</returns>
    public bool IsResetDue(DateTimeOffset now) => ResetDate < now;
    /// <summary>Determines whether the counter has reached the maximum allowed attempts.</summary>
    /// <param name="maxAttempts">The maximum number of allowed attempts.</param>
    /// <returns>True if the counter is maxed out; otherwise, false.</returns>
    public bool IsMaxedOut(int maxAttempts) => Count > maxAttempts;
    /// <summary>Resets the counter to 1 and sets a new reset date based on the provided window. </summary>
    /// <param name="now">The current date and time.</param>
    /// <param name="window">The time span for the new reset window.</param>
    public void Reset(DateTimeOffset now, TimeSpan window) {
        Count = 1;
        ResetDate = now.Add(window);
        LastUpdate = now;
    }
    /// <summary>Increments the counter by 1 and updates the last update timestamp.</summary>
    /// <param name="now">The current date and time.</param>
    public void Increment(DateTimeOffset now) {
        Count++;
        LastUpdate = now;
    }
    /// <summary>
    /// Creates a new instance of <see cref="UserRateCounter"/> with the specified user id, action name, current date, and window.
    /// </summary>
    /// <param name="userId">The user id this counter belongs to.</param>
    /// <param name="actionName">A free-form action name (for example, Sms:ChangePhoneNumber).</param>
    /// <param name="now">The current date and time.</param>
    /// <param name="window">The time span for the new reset window.</param>
    /// <returns>A new instance of <see cref="UserRateCounter"/>.</returns>
    public static UserRateCounter Create(string userId, string actionName, DateTimeOffset now, TimeSpan window) => new() {
        UserId = userId,
        ActionName = actionName,
        Count = 0,
        ResetDate = now.Add(window),
        LastUpdate = now
    };
}