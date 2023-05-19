using Indice.Features.Identity.Core.Data.Models;
using Indice.Services;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a user's email confirmation is resent, using IdentityServer API.</summary>
/// <typeparam name="TUser">The type of user.</typeparam>
public class UserRequestForEmailConfirmationEvent<TUser> : IPlatformEvent where TUser : User
{
    /// <summary>Creates a new instance of <see cref="UserRequestForEmailConfirmationEvent{TUser}"/>.</summary>
    /// <param name="user">The user entity.</param>
    /// <param name="confirmationToken">The generated email confirmation token.</param>
    public UserRequestForEmailConfirmationEvent(TUser user, string confirmationToken) {
        User = user ?? throw new ArgumentNullException(nameof(user));
        ConfirmationToken = confirmationToken ?? throw new ArgumentNullException(nameof(confirmationToken));
    }

    /// <summary>The user entity.</summary>
    public TUser User { get; }
    /// <summary>The generated email confirmation token.</summary>
    public string ConfirmationToken { get; }
}

/// <summary>An event that is raised when a user's email confirmation is resent, using IdentityServer API.</summary>
public class UserRequestForEmailConfirmationEvent : UserRequestForEmailConfirmationEvent<User>
{
    /// <summary>Creates a new instance of <see cref="UserRequestForEmailConfirmationEvent"/>.</summary>
    public UserRequestForEmailConfirmationEvent(User user, string confirmationToken) : base(user, confirmationToken) { }
}