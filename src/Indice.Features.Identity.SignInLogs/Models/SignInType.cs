namespace Indice.Features.Identity.SignInLogs.Models;

/// <summary>Describes the user sign in type in terms of user presence.</summary>
public enum SignInType
{
    /// <summary>User is present during sign in (i.e. enters pass on login screen).</summary>
    Interactive,
    /// <summary>User is not present during sign in (i.e. password is refreshed)</summary>
    NonInteractive
}
