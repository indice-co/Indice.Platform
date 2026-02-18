using Microsoft.AspNetCore.Authentication.OAuth;

namespace Indice.AspNetCore.Authentication.GovGr;

/// <summary>
/// Options for configuring GovGr OAuth authentication extending <see cref="OAuthOptions"/>
/// with support for logout and federated logout behavior.
/// </summary>
public class GovGrOAuthOptions : OAuthOptions
{
    /// <summary>The endpoint used to perform federated logout.</summary>
    public string? LogoutEndpoint { get; set; }
    /// <summary> Indicates whether federated logout is enabled.</summary>
    public bool EnableFederatedLogout { get; set; } = true;
}
