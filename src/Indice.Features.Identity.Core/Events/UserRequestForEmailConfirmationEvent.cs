using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a user's email confirmation is resent, using IdentityServer API.</summary>
/// <remarks>Creates a new instance of <see cref="UserRequestForEmailConfirmationEvent"/>.</remarks>
/// <param name="user">The user context.</param>
/// <param name="confirmationToken">The generated email confirmation token.</param>
public class UserRequestForEmailConfirmationEvent(
    UserEventContext user, 
    string confirmationToken) : IPlatformEvent
{
    /// <summary>The user entity.</summary>
    public UserEventContext User { get; } = user ?? throw new ArgumentNullException(nameof(user));
    /// <summary>The generated email confirmation token.</summary>
    public string ConfirmationToken { get; } = confirmationToken ?? throw new ArgumentNullException(nameof(confirmationToken));
}
