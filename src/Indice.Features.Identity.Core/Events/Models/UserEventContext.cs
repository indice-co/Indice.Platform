using Indice.Features.Identity.Core.Data.Models;

namespace Indice.Features.Identity.Core.Events.Models;

/// <summary>User information about an event that occurred.</summary>
public sealed class UserEventContext
{
    /// <summary>Gets or sets the primary key for this user.</summary>
    public string Id { get; private set; } = null!;
    /// <summary>Gets or sets the user name for this user.</summary>
    public string UserName { get; private set; } = null!;
    /// <summary>Gets or sets the email address for this user.</summary>
    public string? Email { get; private set; }
    /// <summary>Gets or sets a flag indicating if a user has confirmed their email address.</summary>
    public bool EmailConfirmed { get; private set; }
    /// <summary>Gets or sets a telephone number for the user.</summary>
    public string? PhoneNumber { get; private set; }
    /// <summary>Gets or sets a flag indicating if a user has confirmed their telephone address.</summary>
    public bool PhoneNumberConfirmed { get; private set; }
    /// <summary>Gets or sets a flag indicating if two factor authentication is enabled for this user.</summary>
    public bool TwoFactorEnabled { get; private set; }
    /// <summary>Gets or sets the date and time, in UTC, when any user lockout ends.</summary>
    public DateTimeOffset? LockoutEnd { get; private set; }
    /// <summary>Gets or sets a flag indicating if the user could be locked out.</summary>
    public bool LockoutEnabled { get; private set; }
    /// <summary>Gets or sets the number of failed login attempts for the current user.</summary>
    public int AccessFailedCount { get; private set; }
    /// <summary>Date that the user was created.</summary>
    public DateTimeOffset CreateDate { get; private set; }
    /// <summary>Gets or sets the date and time, in UTC, when the user last signed in.</summary>
    public DateTimeOffset? LastSignInDate { get; private set; }
    /// <summary>Date that represents the last time the user changed his password.</summary>
    public DateTimeOffset? LastPasswordChangeDate { get; private set; }
    /// <summary>Represents the password expiration policy the value is measured in days.</summary>
    public PasswordExpirationPolicy? PasswordExpirationPolicy { get; private set; }
    /// <summary>If set, it represents the date when the current password will expire.</summary>
    public DateTimeOffset? PasswordExpirationDate { get; private set; }
    /// <summary>System administrator indicator.</summary>
    public bool Admin { get; private set; }
    /// <summary>Indicates whether the user is forcefully blocked.</summary>
    public bool Blocked { get; private set; }
    /// <summary>Indicates whether the user must provide a new password upon next login.</summary>
    public bool PasswordExpired { get; private set; }
    /// <summary>Navigation property for the claims this user possesses.</summary>
    public List<UserClaimEventContext> Claims { get; private set; } = [];

    /// <summary>Creates a new <see cref="UserEventContext"/> instance given a <see cref="User"/> entity.</summary>
    /// <param name="user">The user entity.</param>
    public static UserEventContext InitializeFromUser(User user) => new() {
        AccessFailedCount = user.AccessFailedCount,
        Admin = user.Admin,
        Blocked = user.Blocked,
        Claims = user.Claims?.Select(claim => new UserClaimEventContext(claim.ClaimType!, claim.ClaimValue!)).ToList() ?? [],
        CreateDate = user.CreateDate,
        Email = user.Email,
        EmailConfirmed = user.EmailConfirmed,
        Id = user.Id,
        LastPasswordChangeDate = user.LastPasswordChangeDate,
        LastSignInDate = user.LastSignInDate,
        LockoutEnabled = user.LockoutEnabled,
        LockoutEnd = user.LockoutEnd,
        PasswordExpirationDate = user.PasswordExpirationDate,
        PasswordExpirationPolicy = user.PasswordExpirationPolicy,
        PasswordExpired = user.PasswordExpired,
        PhoneNumber = user.PhoneNumber,
        PhoneNumberConfirmed = user.PhoneNumberConfirmed,
        TwoFactorEnabled = user.TwoFactorEnabled,
        UserName = user.UserName!
    };
}

/// <summary>User claim.</summary>
/// <remarks>Creates a new instance of <see cref="UserClaimEventContext"/>.</remarks>
/// <param name="type">The claim type for this claim.</param>
/// <param name="value">The claim value for this claim.</param>
public class UserClaimEventContext(string type, string value)
{
    /// <summary>The claim type for this claim.</summary>
    public string Type { get; } = type;
    /// <summary>The claim value for this claim.</summary>
    public string Value { get; } = value;
}
