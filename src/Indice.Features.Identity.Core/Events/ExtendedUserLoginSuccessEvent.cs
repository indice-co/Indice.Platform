using IdentityServer4.Events;

namespace Indice.Features.Identity.Core.Events;

/// <summary>Event for successful user authentication.</summary>
public class ExtendedUserLoginSuccessEvent : UserLoginSuccessEvent
{
    /// <summary>Creates a new instance of <see cref="ExtendedUserLoginSuccessEvent"/>.</summary>
    public ExtendedUserLoginSuccessEvent() : base() { }

    /// <summary>Creates a new instance of <see cref="ExtendedUserLoginSuccessEvent"/>.</summary>
    /// <param name="provider">The provider.</param>
    /// <param name="providerUserId">The provider user identifier.</param>
    /// <param name="subjectId">The subject identifier.</param>
    /// <param name="name">The name.</param>
    /// <param name="interactive">if set to <c>true</c> [interactive].</param>
    /// <param name="clientId">The client id.</param>
    public ExtendedUserLoginSuccessEvent(
        string provider,
        string providerUserId,
        string subjectId,
        string name,
        bool interactive = true,
        string clientId = null) : base(provider, providerUserId, subjectId, name, interactive, clientId) { }

    /// <summary>Creates a new instance of <see cref="ExtendedUserLoginSuccessEvent"/>.</summary>
    /// <param name="username">The username.</param>
    /// <param name="subjectId">The subject identifier.</param>
    /// <param name="name">The name.</param>
    /// <param name="interactive">if set to <c>true</c> [interactive].</param>
    /// <param name="clientId">The client id.</param>
    /// <param name="warning">Describes a warning that may occur during a sign in event.</param>
    public ExtendedUserLoginSuccessEvent(
        string username,
        string subjectId,
        string name,
        bool interactive = true,
        string clientId = null,
        SignInWarning? warning = null) : base(username, subjectId, name, interactive, clientId) {
        Warning = warning;
    }

    /// <summary>Describes a warning that may occur during a sign in event.</summary>
    public SignInWarning? Warning { get; }
}
