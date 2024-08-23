using IdentityServer4.Events;

namespace Indice.Features.Identity.Core.Events;

/// <summary>Event for successful user password authentication.</summary>
public class UserPasswordLoginSuccessEvent : Event
{
    /// <summary>Creates a new instance of the <see cref="UserLoginSuccessEvent"/> class.</summary>
    public UserPasswordLoginSuccessEvent() : base(EventCategories.Authentication, "User Password Login Success", EventTypes.Success, 6000) { }

    /// <summary>Creates a new instance of the <see cref="UserLoginSuccessEvent"/> class.</summary>
    /// <param name="username">The username.</param>
    /// <param name="subjectId">The subject identifier.</param>
    /// <param name="name">The name.</param>
    /// <param name="interactive">if set to <c>true</c> [interactive].</param>
    /// <param name="clientId">The client id.</param>
    /// <param name="clientName">The client name.</param>
    /// <param name="warning">Describes a warning that may occur during a sign in event.</param>
    public UserPasswordLoginSuccessEvent(
        string username,
        string subjectId,
        string name,
        bool interactive = true,
        string? clientId = null,
        string? clientName = null,
        SignInWarning? warning = null
    ) : this() {
        Username = username;
        SubjectId = subjectId;
        DisplayName = name;
        ClientId = clientId;
        ClientName = clientName;
        Endpoint = interactive ? "UI" : "Token";
        Warning = warning;
    }

    /// <summary>Gets the username.</summary>
    public string Username { get; }
    /// <summary>Gets the subject identifier.</summary>
    public string SubjectId { get; }
    /// <summary>Gets the display name.</summary>
    public string DisplayName { get; }
    /// <summary>Gets the endpoint.</summary>
    public string Endpoint { get; }
    /// <summary>Gets the client id.</summary>
    public string? ClientId { get; }
    /// <summary>Gets the client id.</summary>
    public string? ClientName { get; }
    /// <summary>Describes a warning that may occur during a sign in event.</summary>
    public SignInWarning? Warning { get; }
}
