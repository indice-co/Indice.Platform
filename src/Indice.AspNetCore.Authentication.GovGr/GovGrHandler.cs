using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Linq;
using Indice.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
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
        UrlEncoder encoder ) 
        : base(options, logger, encoder)
    {
    }
    private const string MissingNameClaimError = "The response from GovGr did not contain a name claim required for the subject.";


    /// <summary>
    /// Creates an authentication ticket for the user.
    /// </summary>
    /// <param name="identity">The claims identity.</param>
    /// <param name="properties">The authentication properties.</param>
    /// <param name="tokens">The OAuth token response.</param>
    protected override async Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens) {

        using (var user = JsonDocument.Parse("{}")) {
            var context = new OAuthCreatingTicketContext(new ClaimsPrincipal(identity), properties, Context, Scheme, Options, Backchannel, tokens, user.RootElement);
            var accessToken = context.Properties.GetTokenValue("access_token");
            var httpClient = context.Backchannel;
            using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            var xml = XDocument.Parse(responseBody);
            var claims = xml.Descendants("userinfo")
                            .SelectMany(x => x.Attributes()
                                              .Select(attr => new Claim(GovGrExtensions.GovGrClaimMap.GetValueOrDefault(attr.Name.LocalName, attr.Name.LocalName), attr.Value.Trim())))
                            .Where(x => !GovGrExtensions.GovGrClaimNullLiteral.Equals(x.Value, StringComparison.OrdinalIgnoreCase))
                            .ToList();
            // add another claim for subject since this is not available.
            var nameClaim = claims.Find(x => x.Type == BasicClaimTypes.Name)
                ?? throw new AuthenticationFailureException(MissingNameClaimError);
            claims.Add(new Claim(BasicClaimTypes.Subject, nameClaim.Value));
            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name, BasicClaimTypes.Name, BasicClaimTypes.Role));
            await Events.CreatingTicket(context);
            return new AuthenticationTicket(context.Principal!, context.Properties, Scheme.Name);
        }
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
        var request = Context.Request;
        var absoluteReturnUrl = $"{request.Scheme}://{request.Host}{postLogoutRedirectUri}";
        var logoutEndpoint = Options.LogoutEndpoint.TrimEnd('/');
        var clientId = Options.ClientId;
        var logoutUrl = $"{logoutEndpoint}/{clientId}/?url={Uri.EscapeDataString(absoluteReturnUrl)}";
        Context.Response.Redirect(logoutUrl);
    }


    /// <summary>
    /// Checks if the URL is local to prevent open redirect attacks.
    /// </summary>
    private static bool IsLocalUrl(string url) {
        if (string.IsNullOrEmpty(url)) return false;

        // Reject URLs starting with // or /\ (protocol-relative or escaped)
        if (url.Length > 1 && url[0] == '/' && (url[1] == '/' || url[1] == '\\'))
            return false;

        // Accept absolute paths starting with /
        if (url[0] == '/') return true;

        // Accept app-relative paths ~/
        if (url.Length > 1 && url[0] == '~' && url[1] == '/') return true;

        return false;
    }

}
