using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>
/// An event that is raised when a password reset email is requested: by someone entering an email address (self-service forgot password,
/// Identity UI page and API) or by an administrator sending a password reset email to a user. Self-service requests raise it for every
/// outcome, so requests for unknown email addresses (possible account enumeration) and requests over the email limit are audited too.
/// </summary>
/// <remarks>Creates a new instance of <see cref="PasswordResetRequestEvent"/>.</remarks>
/// <param name="email">The email address that was entered, or the user's email address when <paramref name="initiatedByAdmin"/> is <see langword="true"/>.</param>
/// <param name="outcome">The outcome of the request.</param>
/// <param name="user">The user context, when an account with the <paramref name="email"/> exists.</param>
/// <param name="initiatedByAdmin">Whether an administrator sent the password reset email to the user.</param>
public class PasswordResetRequestEvent(string email, PasswordResetRequestOutcome outcome, UserEventContext? user = null, bool initiatedByAdmin = false) : IPlatformEvent
{
    /// <summary>The email address that was entered, or the user's email address when <see cref="InitiatedByAdmin"/> is <see langword="true"/>.</summary>
    public string Email { get; } = email;

    /// <summary>The outcome of the request.</summary>
    public PasswordResetRequestOutcome Outcome { get; } = outcome;

    /// <summary>The user context, when an account with the <see cref="Email"/> exists. <see langword="null"/> when <see cref="Outcome"/> is <see cref="PasswordResetRequestOutcome.UserNotFound"/>.</summary>
    public UserEventContext? User { get; } = user;

    /// <summary>Whether an administrator sent the password reset email to the user, instead of the user asking for it.</summary>
    public bool InitiatedByAdmin { get; } = initiatedByAdmin;

    /// <summary>Whether the password reset email was sent.</summary>
    public bool Succeeded => Outcome == PasswordResetRequestOutcome.EmailSent;
}

/// <summary>The outcome of a <see cref="PasswordResetRequestEvent"/>.</summary>
public enum PasswordResetRequestOutcome
{
    /// <summary>The password reset email was sent.</summary>
    EmailSent,
    /// <summary>No account has the email address that was entered. No email was sent.</summary>
    UserNotFound,
    /// <summary>The account has reached its password reset email limit (<see cref="Guards.ActionRateLimiterActionNames.ForgotPassword"/>). No email was sent.</summary>
    LimitReached
}
