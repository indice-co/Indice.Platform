namespace Indice.Features.Identity.Core;

/// <summary>Describes the MFA policy for new users.</summary>
public enum MfaPolicy : short
{
    /// <summary>Optional driven by database flag on User entity.</summary>
    /// <remarks>This means that the user can opt in via the profile page. Default behavior</remarks>
    Optional = 0,
    /// <summary>MFA enforced for newly created users.</summary>
    Enforced = 1,
    /// <summary>Synonym of optional</summary>
    Default = Optional,
}
