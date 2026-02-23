using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Authentication.GovGr;

/// <summary>
/// Custom authentication handler for GovGr that supports federated sign-out.
/// </summary>
public class GovGrHandler : OAuthHandler<GovGrOptions>, IAuthenticationSignOutHandler
{
    /// <summary>
    /// Initializes a new instance of <see cref="GovGrHandler"/>.
    /// </summary>
    public GovGrHandler(
        IOptionsMonitor<GovGrOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) 
        : base(options, logger, encoder)
    {
    }

    /// <summary>
    /// Handles federated sign-out by redirecting to GovGr's logout endpoint.
    /// </summary>
    /// <param name="properties">Authentication properties that may contain redirect information.</param>
    public async Task SignOutAsync(AuthenticationProperties? properties) {

        var postLogoutRedirectUri = properties?.RedirectUri;
        if (string.IsNullOrWhiteSpace(postLogoutRedirectUri) || !IsLocalUrl(postLogoutRedirectUri)) {
            postLogoutRedirectUri = "/";
        }
        if (!Options.EnableFederatedLogout || string.IsNullOrWhiteSpace(Options.LogoutEndpoint)) {
            Context.Response.Redirect(postLogoutRedirectUri);
            return;
        }
        postLogoutRedirectUri = UriHelper.BuildAbsolute(Context.Request.Scheme, Context.Request.Host, path: postLogoutRedirectUri);
        var logoutEndpoint = Options.LogoutEndpoint;
        var clientId = Options.ClientId;
        var logoutUrl = $"{logoutEndpoint}/{clientId}/?url={Uri.EscapeDataString(postLogoutRedirectUri)}";
        Context.Response.Redirect(logoutUrl);

        await Task.CompletedTask;
    }

    private bool IsLocalUrl(string url) {
        if (string.IsNullOrWhiteSpace(url)) return false;
        return (url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\')))
               || (url.Length > 1 && url[0] == '~' && url[1] == '/');
    }
}
