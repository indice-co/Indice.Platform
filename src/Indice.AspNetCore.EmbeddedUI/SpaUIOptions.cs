using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.StaticFiles;

namespace Indice.AspNetCore.EmbeddedUI;

/// <summary>Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</summary>
public class SpaUIOptions
{
    private string _pathPrefix = "/";

    /// <summary>The name of the section used in appsettings.json file.</summary>
    public const string SectionName = "SpaUI";
    /// <summary>The base address of the authority server (i.e. https://identity.example.com).</summary>
    public string Authority { get; set; }
    /// <summary>The client id used to identify the application in the authority server. Defaults to <b>spa-ui</b>.</summary>
    public string ClientId { get; set; } = "spa-ui";
    /// <summary>The default scope names, as space separated values, to be used for Campaigns API. This can include the main scope (audience) and sub-scopes. Defaults to <b>null</b>.</summary>
    public string Scope { get; set; }
    /// <summary>Specifies the title (shown in browser tab) used in the application. Defaults to <b>Indice BackOffice UI</b>.</summary>
    public string DocumentTitle { get; set; } = "Indice BackOffice UI";
    /// <summary>Gets or sets additional content to place in the head of the SPA page.</summary>
    public string HeadContent { get; set; }
    /// <summary>Specifies whether the application is served in the specified path, as dictated by the <see cref="PathPrefixPattern"/> property. Can be used in scenarios where the application needs to be hidden. Defaults to true.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>The base address of the application host (i.e. https://example.com).</summary>
    public string Host { get; set; }
    /// <summary>Where is the main api that drives this ui. Defaults to <strong>/api</strong> which is the same origin. </summary>
    /// <remarks>Can also be pointed to a an exteranl origin ei: messaging.indice.gr.</remarks>
    public string ApiBase { get; set; } = "/api";
    /// <summary>URI to redirect to after logout.</summary>
    public string PostLogoutRedirectUri { get; set; }
    /// <summary>Called after the status code and headers have been set, but before the body has been written. This can be used to add or change the response headers.</summary>
    public Action<StaticFileResponseContext> OnPrepareResponse { get; set; }
    /// <summary>Provides a way to configure extra custom parameters for the index.html page.</summary>
    public Action<Dictionary<string, string>> ConfigureIndexParameters { get; set; }
    /// <summary></summary>
    public Func<HttpContext, IDictionary<string, string>, string> TenantIdAccessor { get; set; }
    /// <summary></summary>
    public string PathPrefix {
        get { return _pathPrefix; }
        set {
            _pathPrefix = !string.IsNullOrWhiteSpace(value) ? value : _pathPrefix;
            PathPrefixPattern = RoutePatternFactory.Parse(_pathPrefix);
        }
    }
    
    internal string Version { get; set; }
    internal bool Multitenancy => TenantIdAccessor is not null;
    internal string TenantId { get; set; }
    internal string Path { get; set; }
    internal RoutePattern PathPrefixPattern { get; set; } = RoutePatternFactory.Parse("/");
}
