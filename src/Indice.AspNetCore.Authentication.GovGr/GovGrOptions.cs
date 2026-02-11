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
public class GovGrOptions
{
    private const string FQDN_STAGE = "test.gsis.gr";
    private const string FQDN_PROD = "oauth2.gsis.gr";
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
    public List<string> Scopes { get; } = [GovGrScope.Read];

    /// <summary>Check if in production</summary>
    public bool IsProduction => string.IsNullOrWhiteSpace(Environment) || "Production".Equals(Environment, StringComparison.OrdinalIgnoreCase);
    /// <summary>Check if in staging/stage</summary>
    public bool IsStaging => "Staging".Equals(Environment, StringComparison.OrdinalIgnoreCase) || "Stage".Equals(Environment, StringComparison.OrdinalIgnoreCase) || "test".Equals(Environment, StringComparison.OrdinalIgnoreCase);

    internal string BaseDomain => IsStaging ? FQDN_STAGE : FQDN_PROD;
    /// <summary>The authority.</summary>
    public string Authority => $"https://{BaseDomain}/oauth2server";
    /// <summary>The default endpoint used to perform gov.gr authentication.</summary>
    public string AuthorizationEndpoint => $"{Authority}/oauth/authorize";
    /// <summary>The OAuth endpoint used to exchange access tokens.</summary>
    public string TokenEndpoint => $"{Authority}/oauth/token";
    /// <summary>The OAuth endpoint used to exchange access tokens.</summary>
    public string UserInfoEndpoint => $"{Authority}/userinfo?format=xml";

    /// <summary>
    /// Gets or sets timeout value in milliseconds for back channel communications with
    /// the remote identity provider.
    /// </summary>
    public TimeSpan BackchannelTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the HttpMessageHandler used to communicate with remote identity provider. This 
    /// cannot be set at the same time as BackchannelCertificateValidator unless the
    /// value can be downcast to a WebRequestHandler.
    /// </summary>
    public HttpMessageHandler? BackchannelHttpHandler { get; set; }
    /// <summary>
    /// Used to communicate with the remote identity provider. 
    /// </summary>
    public HttpClient Backchannel { get; set; } = null!;

    /// <summary>
    /// Gets or sets the event handlers used to process OAuth authentication events.
    /// </summary>
    /// <remarks>Assign a custom <see cref="OAuthEvents"/> instance to handle specific authentication events.
    /// Currently, it will only configure the handler of OnRemoteFailure. If not set, default event handlers are used.</remarks>
    public OAuthEvents Events { get; set; } = new OAuthEvents();
}
