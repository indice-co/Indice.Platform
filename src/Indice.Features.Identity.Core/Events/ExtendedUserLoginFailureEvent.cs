using IdentityServer4.Events;

namespace Indice.Features.Identity.Core.Events;

/// <summary>Event for failed user authentication.</summary>
public class ExtendedUserLoginFailureEvent : UserLoginFailureEvent
{
    /// <summary>Creates a new instance of <see cref="ExtendedUserLoginFailureEvent"/>.</summary>
    /// <param name="username">The username.</param>
    /// <param name="error">The error.</param>
    /// <param name="interactive">Specifies if login was interactive</param>
    /// <param name="clientId">The client id</param>
    /// <param name="subjectId">The subject id.</param>
    public ExtendedUserLoginFailureEvent(
        string username, 
        string error, 
        bool interactive = true, 
        string clientId = null, 
        string subjectId = null
    ) : base(username, error, interactive, clientId) { 
        SubjectId = subjectId;
    }

    /// <summary>The subject id.</summary>
    public string SubjectId { get; }
}
