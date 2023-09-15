namespace IdentityServer4.Events;

/// <summary>Extends the </summary>
public class ExtendedUserLoginSuccessEvent : UserLoginSuccessEvent
{
    /// <summary>Creates a new instance of <see cref="ExtendedUserLoginSuccessEvent"/>.</summary>
    public ExtendedUserLoginSuccessEvent() : base() { }

    /// <summary>Creates a new instance of <see cref="ExtendedUserLoginSuccessEvent"/>.</summary>
    /// <param name="provider">The provider.</param>
    /// <param name="providerUserId">The provider user identifier.</param>
    /// <param name="subjectId">The subject identifier.</param>
    /// <param name="displayName">The name.</param>
    /// <param name="interactive">if set to <c>true</c> [interactive].</param>
    /// <param name="clientId">The client id.</param>
    public ExtendedUserLoginSuccessEvent(
        string provider,
        string providerUserId,
        string subjectId,
        string displayName,
        bool interactive = true,
        string clientId = null) : base(provider, providerUserId, subjectId, displayName, interactive, clientId) { }

    /// <summary>Creates a new instance of <see cref="ExtendedUserLoginSuccessEvent"/>.</summary>
    /// <param name="username">The username.</param>
    /// <param name="subjectId">The subject identifier.</param>
    /// <param name="displayName">The name.</param>
    /// <param name="interactive">if set to <c>true</c> [interactive].</param>
    /// <param name="clientId">The client id.</param>
    public ExtendedUserLoginSuccessEvent(
        string username,
        string subjectId,
        string displayName,
        bool interactive = true,
        string clientId = null) : base(username, subjectId, displayName, interactive, clientId) { }
}
