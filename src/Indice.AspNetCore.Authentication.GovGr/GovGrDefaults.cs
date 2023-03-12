namespace Indice.AspNetCore.Authentication.GovGr;

/// <summary>Default values for authenticating using gov.gr.</summary>
public class GovGrDefaults
{
    /// <summary>The default scheme for gov.gr authentication. Defaults to <c>GovGr</c>.</summary>
    public const string AuthenticationScheme = "GovGr";
    /// <summary>The default display name for gov.gr authentication. Defaults to <c>Sign in with GovGr</c>.</summary>
    public static readonly string DisplayName = "Sign in with GovGr";
    /// <summary>The default endpoint used to perform gov.gr authentication.</summary>
    public static readonly string AuthorizationEndpoint = "https://kyc.gov.gr/oauth";
    /// <summary>The OAuth endpoint used to exchange access tokens.</summary>
    public static readonly string TokenEndpoint = "https://kyc.gov.gr/oauth/token";
    /// <summary>The OAuth endpoint used to discover OpenId connect supported features.</summary>
    public static readonly string DiscoveryEndpoint = string.Empty;
    /// <summary>The authority.</summary>
    public static readonly string Authority = "https://kyc.gov.gr";
    /// <summary>No user info endpoint is provided, which means all of the claims about users have to be included in the (expiring and potentially large) id_token.</summary>
    public static readonly string UserInformationEndpoint = string.Empty;
}
