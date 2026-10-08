#if NET9_0_OR_GREATER
using Duende.IdentityServer.Events;
#else
using IdentityServer4.Events;
#endif

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
        string? clientId = null) : base(provider, providerUserId, subjectId, name, interactive, clientId) { }

    /// <summary>Creates a new instance of <see cref="ExtendedUserLoginSuccessEvent"/>.</summary>
    /// <param name="username">The username.</param>
    /// <param name="subjectId">The subject identifier. Populate if external authentication happened</param>
    /// <param name="name">The name.</param>
    /// <param name="provider">The idp provider.</param>
    /// <param name="interactive">if set to <c>true</c> [interactive].</param>
    /// <param name="clientId">The client id.</param>
    /// <param name="clientName">The client name.</param>
    /// <param name="warning">Describes a warning that may occur during a sign in event.</param>
    /// <param name="sessionId">User's session id</param>
    /// <param name="authenticationMethods">List of authentication methods used.</param>
    public ExtendedUserLoginSuccessEvent(
        string username,
        string subjectId,
        string name,
        string? provider = null,
        bool interactive = true,
        string? clientId = null,
        string? clientName = null,
        SignInWarning? warning = null,
        string? sessionId = null,
        string[]? authenticationMethods = null
    ) : base(username, subjectId, name, interactive, clientId) {
        Provider = provider;
        ClientName = clientName;
        Warning = warning;
        SessionId = sessionId;
        AuthenticationMethods = authenticationMethods ?? [];
    }

    /// <summary>Describes a warning that may occur during a sign in event.</summary>
    public SignInWarning? Warning { get; }
    /// <summary>The client name.</summary>
    public string? ClientName { get; }
    /// <summary>User's session id.</summary>
    public string? SessionId { get; set; }
    /// <summary>List of authentication methods used.</summary>
    public string[] AuthenticationMethods { get; } = [];
}
