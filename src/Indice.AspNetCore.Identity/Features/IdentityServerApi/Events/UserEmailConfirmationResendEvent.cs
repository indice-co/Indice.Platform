using System;
using Indice.Events;
using Indice.Features.Identity.Core.Data.Models;

namespace Indice.AspNetCore.Identity.Api.Events;

/// <summary>An event that is raised when a user's email confirmation is resent, using IdentityServer API.</summary>
/// <typeparam name="TUser">The type of user.</typeparam>
public class UserEmailConfirmationResendEvent<TUser> : IPlatformEvent where TUser : User
{
    /// <summary>Creates a new instance of <see cref="UserEmailConfirmationResendEvent{TUser}"/>.</summary>
    /// <param name="user">The user entity.</param>
    /// <param name="confirmationToken">The generated email confirmation token.</param>
    public UserEmailConfirmationResendEvent(TUser user, string confirmationToken) {
        User = user ?? throw new ArgumentNullException(nameof(user));
        ConfirmationToken = confirmationToken ?? throw new ArgumentNullException(nameof(confirmationToken));
    }

    /// <summary>The user entity.</summary>
    public TUser User { get; }
    /// <summary>The generated email confirmation token.</summary>
    public string ConfirmationToken { get; }
}

/// <summary>An event that is raised when a user's email confirmation is resent, using IdentityServer API.</summary>
public class UserEmailConfirmationResendEvent : UserEmailConfirmationResendEvent<User>
{
    /// <summary>Creates a new instance of <see cref="UserEmailConfirmationResendEvent"/>.</summary>
    public UserEmailConfirmationResendEvent(User user, string confirmationToken) : base(user, confirmationToken) { }
}
