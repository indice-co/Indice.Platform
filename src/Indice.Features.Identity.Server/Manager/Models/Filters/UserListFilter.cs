namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Contains filter when querying for users list.</summary>
public class UserListFilter
{
    /// <summary>The type of the claim.</summary>
    public string? ClaimType { get; set; }
    /// <summary>The value of the claim.</summary>
    public string? ClaimValue { get; set; }
    /// <summary>A list of user identifiers to search for.</summary>
    public string[]? UserId { get; set; } = Array.Empty<string>();
    /// <summary>Filters users by whether an administrator has blocked them.</summary>
    public bool? Blocked { get; set; }
    /// <summary>Filters users by whether they are currently locked out after failed sign in attempts.</summary>
    public bool? LockedOut { get; set; }
    /// <summary>Filters users by whether they have verified their email address.</summary>
    public bool? EmailConfirmed { get; set; }
    /// <summary>Filters users by whether they have verified their phone number.</summary>
    public bool? PhoneNumberConfirmed { get; set; }
    /// <summary>Filters users by whether they have enabled two factor authentication.</summary>
    public bool? TwoFactorEnabled { get; set; }
}
