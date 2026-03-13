namespace Indice.Features.ActivityLogs.Models;

/// <summary>Describes the user activity type in terms of user presence.</summary>
public enum ActivityType
{
    /// <summary>User is present during activity (i.e. enters pass on login screen).</summary>
    Interactive,
    /// <summary>User is not present during activity (i.e. password is refreshed)</summary>
    NonInteractive
}
