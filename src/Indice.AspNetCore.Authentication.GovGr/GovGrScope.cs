namespace Indice.AspNetCore.Authentication.GovGr;

/// <summary>
/// Scope options for the gov gr gsis auth server
/// </summary>
public static class GovGrScope
{
    /// <summary>Access to user id</summary>
    public const string OpenId = "openid";
    /// <summary>Provides access to read the user info</summary>
    public const string Read = "read";
}
