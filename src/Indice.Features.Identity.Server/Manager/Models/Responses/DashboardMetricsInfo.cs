namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Key figures shown on the Admin UI dashboard.</summary>
public class DashboardMetricsInfo
{
    /// <summary>Indicates the point in time where the metrics were calculated.</summary>
    public DateTimeOffset CalculatedAt { get; set; }
    /// <summary>User figures. Present only when the caller can read users.</summary>
    public UserMetricsInfo? Users { get; set; }
    /// <summary>Client and resource figures. Present only when the caller can read clients.</summary>
    public ClientMetricsInfo? Clients { get; set; }
}

/// <summary>User figures for the dashboard.</summary>
public class UserMetricsInfo
{
    /// <summary>The total number of users.</summary>
    public int Total { get; set; }
    /// <summary>The total number of roles.</summary>
    public int Roles { get; set; }
    /// <summary>Users created in the last 7 days.</summary>
    public int NewLast7Days { get; set; }
    /// <summary>Users created in the last 30 days.</summary>
    public int NewLast30Days { get; set; }
    /// <summary>Users that signed in today (UTC).</summary>
    public int ActiveToday { get; set; }
    /// <summary>Users that signed in yesterday (UTC).</summary>
    public int ActiveYesterday { get; set; }
    /// <summary>Users that signed in during the last 7 days.</summary>
    public int ActiveLast7Days { get; set; }
    /// <summary>Users that signed in during the last 30 days.</summary>
    public int ActiveLast30Days { get; set; }
    /// <summary>Users that have never signed in.</summary>
    public int NeverSignedIn { get; set; }
    /// <summary>Users blocked by an administrator.</summary>
    public int Blocked { get; set; }
    /// <summary>Users currently locked out after failed sign in attempts.</summary>
    public int LockedOut { get; set; }
    /// <summary>Users with two factor authentication enabled.</summary>
    public int TwoFactorEnabled { get; set; }
    /// <summary>Users with a verified email address.</summary>
    public int EmailConfirmed { get; set; }
    /// <summary>Users with a verified phone number.</summary>
    public int PhoneNumberConfirmed { get; set; }
    /// <summary>Users whose password has expired.</summary>
    public int PasswordExpired { get; set; }
}

/// <summary>Client and resource figures for the dashboard.</summary>
public class ClientMetricsInfo
{
    /// <summary>The total number of clients.</summary>
    public int Total { get; set; }
    /// <summary>Clients that are currently enabled.</summary>
    public int Enabled { get; set; }
    /// <summary>The total number of identity resources.</summary>
    public int IdentityResources { get; set; }
    /// <summary>The total number of API resources.</summary>
    public int ApiResources { get; set; }
    /// <summary>The total number of API scopes.</summary>
    public int ApiScopes { get; set; }
}
