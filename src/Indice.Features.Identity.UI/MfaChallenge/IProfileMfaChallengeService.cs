using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Models;
using Indice.Services;

namespace Indice.Features.Identity.UI;

/// <summary>
/// Strong customer authentication (MFA challenge) before a user changes sensitive profile data (email, phone number and username) in the Identity UI.
/// The check applies only when the user has MFA enabled and is satisfied by a short lived cookie issued after a successful code verification.
/// </summary>
public interface IProfileMfaChallengeService
{
    /// <summary>Indicates whether the user may pick a different method than the preferred one, following the same rule as the MFA login page.</summary>
    bool AllowMethodSelection { get; }
    /// <summary>Resolves whether the current request needs an MFA challenge for the given user.</summary>
    /// <param name="user">The signed in user.</param>
    Task<ProfileMfaChallengeStatus> GetStatusAsync(User user);
    /// <summary>Gets the methods that can be used for the MFA challenge, ordered by security level (most secure first).</summary>
    /// <param name="user">The signed in user.</param>
    Task<AuthenticationMethod[]> GetMethodsAsync(User user);
    /// <summary>Finds the method to use. Falls back to the preferred method when <paramref name="code"/> is empty, unknown or method selection is not allowed.</summary>
    /// <param name="user">The signed in user.</param>
    /// <param name="code">The requested method code.</param>
    Task<AuthenticationMethod?> FindMethodAsync(User user, string? code);
    /// <summary>Sends an MFA challenge code through the delivery channel of <paramref name="method"/>. Methods without a delivery channel (authenticator app) need no sending.</summary>
    /// <param name="user">The signed in user.</param>
    /// <param name="method">The selected method.</param>
    Task<TotpResult> SendCodeAsync(User user, AuthenticationMethod method);
    /// <summary>Verifies an MFA challenge code. Failed attempts count towards lockout. On success the MFA challenge cookie is issued.</summary>
    /// <param name="user">The signed in user.</param>
    /// <param name="method">The selected method.</param>
    /// <param name="code">The code entered by the user.</param>
    Task<ProfileMfaChallengeVerifyResult> VerifyCodeAsync(User user, AuthenticationMethod method, string code);
    /// <summary>Removes the MFA challenge cookie.</summary>
    Task ClearAsync();
}

/// <summary>The MFA challenge state of the current request.</summary>
public enum ProfileMfaChallengeStatus
{
    /// <summary>The user has no MFA enabled. Profile changes continue without an MFA challenge.</summary>
    NotRequired,
    /// <summary>A valid MFA challenge cookie exists for the current user and login session.</summary>
    Satisfied,
    /// <summary>The user must pass an MFA challenge before changing email, phone number or username.</summary>
    Required,
    /// <summary>The user has MFA enabled but no usable method for the MFA challenge. Changes are blocked.</summary>
    Blocked
}

/// <summary>The result of an MFA challenge code verification.</summary>
public enum ProfileMfaChallengeVerifyResult
{
    /// <summary>The code is valid and the MFA challenge cookie has been issued.</summary>
    Succeeded,
    /// <summary>The code is invalid.</summary>
    Failed,
    /// <summary>The user is locked out.</summary>
    LockedOut
}
