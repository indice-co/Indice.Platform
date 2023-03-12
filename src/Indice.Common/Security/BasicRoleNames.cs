namespace Indice.Security;

/// <summary>Common role names used in all Indice applications.</summary>
public static class BasicRoleNames
{
    /// <summary>System administrator.</summary>
    public const string Administrator = nameof(Administrator);
    /// <summary>Developer.</summary>
    public const string Developer = nameof(Developer);
    /// <summary>Administrator of the Admin UI.</summary>
    public const string AdminUIAdministrator = nameof(AdminUIAdministrator);
    /// <summary>A user that can apply read operations on users and roles of the Admin UI.</summary>
    public const string AdminUIUsersReader = nameof(AdminUIUsersReader);
    /// <summary>A user that can apply read and write operations on users and roles of the Admin UI.</summary>
    public const string AdminUIUsersWriter = nameof(AdminUIUsersWriter);
    /// <summary>A user that can apply read operations on clients and resources of the Admin UI.</summary>
    public const string AdminUIClientsReader = nameof(AdminUIClientsReader);
    /// <summary>A user that can apply read and write operations on clients and resources of the Admin UI.</summary>
    public const string AdminUIClientsWriter = nameof(AdminUIClientsWriter);
    /// <summary>A user that can manage campaigns.</summary>
    public const string CampaignManager = nameof(CampaignManager);
}
