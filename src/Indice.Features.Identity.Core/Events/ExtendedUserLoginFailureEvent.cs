using IdentityServer4.Events;

namespace Indice.Features.Identity.Core.Events;

/// <summary>Event for failed user authentication.</summary>
/// <remarks>Creates a new instance of <see cref="ExtendedUserLoginFailureEvent"/>.</remarks>
/// <param name="username">The username.</param>
/// <param name="error">The error.</param>
/// <param name="interactive">Specifies if login was interactive</param>
/// <param name="clientId">The client id</param>
/// <param name="subjectId">The subject id.</param>
public class ExtendedUserLoginFailureEvent(
    string username,
    string error,
    bool interactive = true,
    string clientId = null,
    string subjectId = null
) : UserLoginFailureEvent(username, error, interactive, clientId)
{
    /// <summary>The subject id.</summary>
    public string SubjectId { get; } = subjectId;
}
