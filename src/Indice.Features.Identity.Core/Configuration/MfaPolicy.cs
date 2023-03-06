namespace Indice.Features.Identity.Core.Configuration;

/// <summary>Describes the MFA policy for new users.</summary>
public enum MfaPolicy
{
    /// <summary>Default behavior, driven by database flag on User entity.</summary>
    Default = 0,
    /// <summary>MFA enforced for newly created users.</summary>
    Enforced = 1
}
