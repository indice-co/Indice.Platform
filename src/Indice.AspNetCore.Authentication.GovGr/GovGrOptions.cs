using Duende.IdentityModel;
using Indice.Features.GovGr;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Authentication.GovGr;

/// <summary>Configuration options for GovGr OpenID Connect.</summary>
public class GovGrOptionsKyc
{
    private const string FQDN_DEMO = "kycdemo.gsis.gr";
    private const string FQDN_STAGE = "kyc-stage.gov.gr";
    private const string FQDN_PROD = "kyc.gov.gr";
    /// <summary>The client id.</summary>
    public string? ClientId { get; set; }
    /// <summary>The client secret.</summary>
    public string? ClientSecret { get; set; }
    /// <summary>The request path within the application's base path where the user-agent will be returned. The middleware will process this request when it arrives.</summary>
    public PathString? CallbackPath { get; set; }
    /// <summary>
    /// Gets or sets the authentication scheme corresponding to the middleware responsible of persisting user's identity after a successful authentication. This value typically
    /// corresponds to a cookie middleware registered in the Startup class. When omitted, <see cref="AuthenticationOptions.DefaultSignInScheme"/> is used as a fallback value.
    /// </summary>
    public string? SignInScheme { get; set; }

    /// <summary>Represents the environment. Valid options are <em>production</em>, <em>staging</em>, <em>development</em> &amp; <em>mock</em>. Defaults to <b>production</b>. </summary>
    public string? Environment { get; set; }

    /// <summary>Default list of scopes needed to access the kyc data. <see cref="GovGrKycScopes"/></summary>
    public List<string> Scopes { get; } = [
        GovGrKycScopes.Identity,
        GovGrKycScopes.Income,
        GovGrKycScopes.ContactInfo,
        GovGrKycScopes.ProfessionalActivity
    ];

    /// <summary>Check if in production</summary>
    public bool IsProduction => string.IsNullOrWhiteSpace(Environment) || "Production".Equals(Environment, StringComparison.OrdinalIgnoreCase);
    /// <summary>Check if in staging/stage</summary>
    public bool IsStaging => "Staging".Equals(Environment, StringComparison.OrdinalIgnoreCase) || "Stage".Equals(Environment, StringComparison.OrdinalIgnoreCase);
    /// <summary>Check if in development/demo</summary>
    public bool IsDevelopment => "Development".Equals(Environment, StringComparison.OrdinalIgnoreCase) || "Dev".Equals(Environment, StringComparison.OrdinalIgnoreCase) || "demo".Equals(Environment, StringComparison.OrdinalIgnoreCase);

    internal string BaseDomain => IsStaging ? FQDN_STAGE :
                                  IsDevelopment ? FQDN_DEMO : FQDN_PROD;
    /// <summary>The authority.</summary>
    public string Authority => $"https://{BaseDomain}";
    /// <summary>The default endpoint used to perform gov.gr authentication.</summary>
    public string AuthorizationEndpoint => $"{Authority}/oauth";
    /// <summary>The OAuth endpoint used to exchange access tokens.</summary>
    public string TokenEndpoint => $"{Authority}/oauth/token";
    /// <summary>The OAuth endpoint used to exchange access tokens.</summary>
    public string UserInfoEndpoint => $"{Authority}/oauth/userinfo";
}
/// <summary>Configuration options for GovGr OpenID Connect.</summary>
public class GovGrOptions : OAuthOptions {

    /// <summary>
    /// Constructs a new instance of GovGrOptions.
    /// </summary>
    public GovGrOptions() {
        Environment = "Production";
    }

    private const string FQDN_STAGE = "test.gsis.gr";
    private const string FQDN_PROD = "oauth2.gsis.gr";
    
    /// <summary>Represents the environment.</summary>
    private string? _environment;
    /// <summary>Represents the environment. Valid options are <em>production</em>, <em>staging</em>, <em>development</em> &amp; <em>mock</em>. Defaults to <b>production</b>. </summary>
    public string? Environment {
        get => _environment;
        set {
            _environment = value;
            UpdateEndpoints();
        }
    }
    /// <summary>Check if in production</summary>
    public bool IsProduction => string.IsNullOrWhiteSpace(Environment) || "Production".Equals(Environment, StringComparison.OrdinalIgnoreCase);
    /// <summary>Check if in staging/stage</summary>
    public bool IsStaging => "Staging".Equals(Environment, StringComparison.OrdinalIgnoreCase) || "Stage".Equals(Environment, StringComparison.OrdinalIgnoreCase) || "test".Equals(Environment, StringComparison.OrdinalIgnoreCase);
    /// <summary> Indicates whether federated logout is enabled.</summary>
    public bool EnableFederatedLogout { get; set; } = true;
    internal string BaseDomain => IsStaging ? FQDN_STAGE : FQDN_PROD;
    /// <summary>The authority.</summary>
    public string Authority => $"https://{BaseDomain}/oauth2server";
    /// <summary>The endpoint used to perform federated logout on the GovGr identity provider.</summary>
    public string LogoutEndpoint => $"{Authority}/logout";
    private void UpdateEndpoints() {
        AuthorizationEndpoint = $"{Authority}/oauth/authorize";
        TokenEndpoint = $"{Authority}/oauth/token";
        UserInformationEndpoint = $"{Authority}/userinfo?format=xml";
    }
}
