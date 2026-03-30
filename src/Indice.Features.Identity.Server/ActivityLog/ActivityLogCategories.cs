namespace Indice.Features.Identity.Server;

/// <summary>
/// Contains constants for activity log categories used in identity-related events.
/// </summary>
public static class ActivityLogCategories
{
    /// <summary>Category for user-related events (create, delete, block, unblock, name/email changes).</summary>
    public const string User = "User";

    /// <summary>Category for authentication-related events (password changes, email/phone confirmations).</summary>
    public const string Authentication = "Authentication";

    /// <summary>Category for device-related events (create, update, delete, trust requests).</summary>
    public const string Device = "Device";

    /// <summary>Category for client/application-related events (create, update, delete).</summary>
    public const string Client = "Client";
}
