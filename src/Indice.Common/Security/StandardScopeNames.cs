namespace Indice.Security;

/// <summary>
/// Standard scope names.
/// </summary>
public static class StandardScopeNames
{
    /// <summary>
    /// Inserts the subject claim into the access_token &amp; allows you to exchange it with the user information Using openid connect "connect/userinfo" endpoint.
    /// </summary>
    public const string OpenId = "openid";
    /// <summary>
    /// Equivelant with refresh tokens. This is a request type of scope. As : I am requesting offline_access.
    /// </summary>
    public const string OfflineAccess = "offline_access";
}
