using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a user fully signs in.</summary>
/// <remarks>Creates a new instance of <see cref="UserLoginEvent"/>.</remarks>
/// <param name="user">The user context.</param>
/// <param name="succeeded">Indicates whether the login was successful or not.</param>
/// <param name="warning">Describes a warning that may occur during a sign in event.</param>
/// <param name="authenticationMethods">List of authentication methods used.</param>
public class UserLoginEvent(
    UserEventContext user, 
    bool succeeded, 
    SignInWarning? warning = null, 
    string[]? authenticationMethods = null) : IPlatformEvent
{
    /// <summary>The user context.</summary>
    public UserEventContext User { get; } = user;
    /// <summary>Indicates whether the login was successful or not.</summary>
    public bool Succeeded { get; } = succeeded;
    /// <summary>Describes a warning that may occur during a sign in event.</summary>
    public SignInWarning? Warning { get; } = warning;
    /// <summary>List of authentication methods used.</summary>
    public string[] AuthenticationMethods { get; set; } = authenticationMethods ?? [];

    /// <summary>Creates a new instance of <see cref="UserLoginEvent"/> and sets the value true to <see cref="Succeeded"/> property.</summary>
    /// <param name="user">The user entity.</param>
    /// <param name="warning">Describes a warning that may occur during a sign in event.</param>
    /// <param name="authenticationMethods">List of authentication methods used.</param>
    public static UserLoginEvent Success(UserEventContext user, SignInWarning? warning = null, string[]? authenticationMethods = null) =>
        new(user, true, warning, authenticationMethods);

    /// <summary>Creates a new instance of <see cref="UserLoginEvent"/> and sets the value false to <see cref="Succeeded"/> property.</summary>
    /// <param name="user">The user entity.</param>
    public static UserLoginEvent Fail(UserEventContext user) => new(user, false);
}
