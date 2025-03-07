namespace Indice.Features.Identity.UI.Telemetry;

using System;
using System.Globalization;
using System.Security.Principal;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

/// <summary>
/// This class helps to inject Application Insights JavaScript snippet into application code.
/// </summary>
internal class AzureMonitorTelemetryJavaScriptSnippet : ITelemetryJavaScriptSnippet
{
    private const string APPLICATIONINSIGHTS_CONNECTION_STRING = "APPLICATIONINSIGHTS_CONNECTION_STRING";
    private const string APPINSIGHTS_INSTRUMENTATIONKEY = "APPINSIGHTS_INSTRUMENTATIONKEY";
    private const string ScriptTagBegin = @"<script type=""text/javascript"">";
    private const string ScriptTagEnd = "</script>";

    /// <summary>JavaScript snippet.</summary>
    private static readonly string Snippet = TelemetryJavascriptResources.JavaScriptSnippet;

    /// <summary>JavaScript authenticated user tracking snippet.</summary>
    private static readonly string AuthSnippet = TelemetryJavascriptResources.JavaScriptAuthSnippet;

    /// <summary> Http context accessor.</summary>
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Configuration instance.</summary>
    private readonly IConfiguration _configuration;

    /// <summary> Weather to print authenticated user tracking snippet.</summary>
    private readonly bool _enableAuthSnippet;

    private readonly JavaScriptEncoder _encoder;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureMonitorTelemetryJavaScriptSnippet"/> class.
    /// </summary>
    /// <param name="configuration">The configuration instance to use.</param>
    /// <param name="httpContextAccessor">Http context accessor to use.</param>
    /// <param name="encoder">Encoder used to encode user identity.</param>
    public AzureMonitorTelemetryJavaScriptSnippet(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        JavaScriptEncoder? encoder = null) {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _enableAuthSnippet = false;
        _encoder = encoder ?? JavaScriptEncoder.Default;
    }

    /// <summary>
    /// Gets the full JavaScript Snippet in HTML script tags with instrumentation key initialized from TelemetryConfiguration.
    /// </summary>
    /// <remarks>This method will evaluate if Telemetry has been disabled in the config and if the instrumentation key was provided by either setting InstrumentationKey or ConnectionString.</remarks>
    /// <returns>JavaScript code snippet with instrumentation key or returns string.Empty if instrumentation key was not set for the application.</returns>
    public string FullScript {
        get {
            if (!IsAvailable()) {
                return string.Empty;
            } else {
                return string.Concat(ScriptTagBegin, ScriptBody, ScriptTagEnd);
            }
        }
    }

    /// <summary>
    /// Gets the JavaScript Snippet body (without HTML script tags) with instrumentation key initialized from TelemetryConfiguration.
    /// </summary>
    /// <returns>JavaScript code snippet with instrumentation key or returns string.Empty if instrumentation key was not set for the application.</returns>
    public string ScriptBody {
        get {
            // Config JS SDK
            string insertConfig;
            if (!string.IsNullOrEmpty(_configuration[APPLICATIONINSIGHTS_CONNECTION_STRING])) {
                insertConfig = string.Format(CultureInfo.InvariantCulture, "connectionString: '{0}'", _configuration[APPLICATIONINSIGHTS_CONNECTION_STRING]);
            } else if (!string.IsNullOrEmpty(_configuration[APPINSIGHTS_INSTRUMENTATIONKEY])) {
                insertConfig = string.Format(CultureInfo.InvariantCulture, "instrumentationKey: '{0}'", _configuration[APPINSIGHTS_INSTRUMENTATIONKEY]);
            } else {
                return string.Empty;
            }

            // Auth Snippet (setAuthenticatedUserContext)
            string insertAuthUserContext = string.Empty;
            if (_enableAuthSnippet) {
                IIdentity? identity = _httpContextAccessor?.HttpContext?.User?.Identity;
                if (identity != null && identity.IsAuthenticated) {
                    string escapedUserName = _encoder.Encode(identity.Name!);
                    insertAuthUserContext = string.Format(CultureInfo.InvariantCulture, AuthSnippet, escapedUserName);
                }
            }

            var snippet = Snippet.Replace("instrumentationKey: \"INSTRUMENTATION_KEY\"", insertConfig);
            // Return snippet
            return string.Concat(snippet, insertAuthUserContext);
        }
    }

    /// <summary>
    /// Determine if we have enough information to build a full script.
    /// </summary>
    /// <returns>Returns true if we can build the JavaScript snippet.</returns>
    private bool IsAvailable() {
        var disableTelemetry = string.IsNullOrEmpty(_configuration[APPLICATIONINSIGHTS_CONNECTION_STRING]) &&
                               string.IsNullOrEmpty(_configuration[APPINSIGHTS_INSTRUMENTATIONKEY]);
        return !disableTelemetry;
    }
}
