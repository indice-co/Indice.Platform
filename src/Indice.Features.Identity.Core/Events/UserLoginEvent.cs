using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a user fully signs in.</summary>
/// <remarks>Creates a new instance of <see cref="UserLoginEvent"/>.</remarks>
/// <param name="user">The user context.</param>
/// <param name="succeeded">Indicates whether the login was successful or not.</param>
/// <param name="sessionId">User's session id.</param>
/// <param name="warning">Describes a warning that may occur during a sign in event.</param>
/// <param name="provider">External provider scheme name. Optional defaults to local</param>
/// <param name="authenticationMethods">List of authentication methods used.</param>
public class UserLoginEvent(
    UserEventContext user,
    bool succeeded, 
    string? sessionId,
    SignInWarning? warning = null,
    string? provider = null,
    string[]? authenticationMethods = null) : IPlatformEvent
{
    /// <summary>The user context.</summary>
    public UserEventContext User { get; } = user;
    /// <summary>
    /// User's session id. 
    /// This is a unique identifier for the user's session and can be used to correlate events that belong to the same session. 
    /// It is typically generated when the user logs in and remains the same until the user logs out or the session expires.
    /// </summary>
    public string? SessionId { get; } = sessionId;
    /// <summary>Gets the name of the provider associated with the current event.</summary>
    public string Provider { get; } = provider ?? "local";
    /// <summary>Indicates whether the login was successful or not.</summary>
    public bool Succeeded { get; } = succeeded;
    /// <summary>Describes a warning that may occur during a sign in event.</summary>
    public SignInWarning? Warning { get; } = warning;
    /// <summary>List of authentication methods used.</summary>
    public string[] AuthenticationMethods { get; set; } = authenticationMethods ?? [];

    /// <summary>Creates a new instance of <see cref="UserLoginEvent"/> and sets the value true to <see cref="Succeeded"/> property.</summary>
    /// <param name="user">The user entity.</param>
    /// <param name="sessionId">The user session id</param>
    /// <param name="warning">Describes a warning that may occur during a sign in event.</param>
    /// <param name="provider">External provider scheme name. Optional defaults to local</param>
    /// <param name="authenticationMethods">List of authentication methods used.</param>
    public static UserLoginEvent Success(UserEventContext user, string sessionId, SignInWarning? warning = null, string? provider = null, string[]? authenticationMethods = null) =>
        new(user, succeeded: true, sessionId, warning, provider, authenticationMethods);

    /// <summary>Creates a new instance of <see cref="UserLoginEvent"/> and sets the value false to <see cref="Succeeded"/> property.</summary>
    /// <param name="user">The user entity.</param>
    public static UserLoginEvent Fail(UserEventContext user) => new(user, succeeded: false, sessionId: null);
}
