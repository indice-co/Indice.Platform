using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a user fully signs in.</summary>
/// <remarks>Creates a new instance of <see cref="UserPasswordLoginEvent"/>.</remarks>
/// <param name="user">The user context.</param>
/// <param name="succeeded">Indicates whether the login was successful or not.</param>
/// <param name="warning">Describes a warning that may occur during a sign in event.</param>
public class UserPasswordLoginEvent(
    UserEventContext user, 
    bool succeeded, 
    SignInWarning? warning = null) : IPlatformEvent
{
    /// <summary>The user context.</summary>
    public UserEventContext User { get; } = user;
    /// <summary>Indicates whether the login was successful or not.</summary>
    public bool Succeeded { get; } = succeeded;
    /// <summary>Describes a warning that may occur during a sign in event.</summary>
    public SignInWarning? Warning { get; } = warning;

    /// <summary>Creates a new instance of <see cref="UserPasswordLoginEvent"/> and sets the value true to <see cref="Succeeded"/> property.</summary>
    /// <param name="user">The user entity.</param>
    /// <param name="warning">Describes a warning that may occur during a sign in event.</param>
    public static UserPasswordLoginEvent Success(UserEventContext user, SignInWarning? warning = null) => new(user, true, warning);
    /// <summary>Creates a new instance of <see cref="UserPasswordLoginEvent"/> and sets the value false to <see cref="Succeeded"/> property.</summary>
    /// <param name="user">The user entity.</param>
    public static UserPasswordLoginEvent Fail(UserEventContext user) => new(user, false);
}
