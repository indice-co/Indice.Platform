#if NET9_0_OR_GREATER
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
#else
using IdentityServer4.Models;
using IdentityServer4.Stores;
#endif
using System.Security.Principal;
using System.Diagnostics;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Indice.Security;

namespace Indice.Features.Identity.UI;

/// <summary>Various helper extension methods.</summary>
public static class UiExtensions
{
    /// <summary>Determines whether the client is configured to use PKCE.</summary>
    /// <param name="store">The store.</param>
    /// <param name="clientId">The client identifier.</param>
    public static async Task<bool> IsPkceClientAsync(this IClientStore store, string clientId) {
        if (!string.IsNullOrWhiteSpace(clientId)) {
            var client = await store.FindEnabledClientByIdAsync(clientId);
            return client?.RequirePkce == true;
        }
        return false;
    }

    /// <summary>Checks if the redirect URI is for a native client.</summary>
    public static bool IsNativeClient(this AuthorizationRequest context) =>
        !context.RedirectUri.StartsWith("https", StringComparison.Ordinal) &&
        !context.RedirectUri.StartsWith("http", StringComparison.Ordinal);

    /// <summary>Renders a loading page that is used to redirect back to the redirectUri.</summary>
    public static IActionResult LoadingPage(this PageModel page, string partialViewName, string redirectUri) {
        page.HttpContext.Response.StatusCode = 200;
        page.HttpContext.Response.Headers["Location"] = string.Empty;
        //return page.RedirectToPage(pageName, new { RedirectUri = redirectUri });
        return page.Partial(partialViewName, new RedirectViewModel { RedirectUri = redirectUri });
    }

    /// <summary>Determines if the authentication scheme support sign out.</summary>
    public static async Task<bool> GetSchemeSupportsSignOutAsync(this HttpContext context, string scheme) {
        var provider = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
        var handler = await provider.GetHandlerAsync(context, scheme);
        return handler is IAuthenticationSignOutHandler;
    }

    /// <summary>
    /// Determines whether this instance is authenticated.
    /// </summary>
    /// <param name="principal">The principal.</param>
    /// <returns>
    ///   <c>true</c> if the specified principal is authenticated; otherwise, <c>false</c>.
    /// </returns>
    [DebuggerStepThrough]
    public static bool IsAuthenticated(this IPrincipal principal) 
        => principal != null && principal.Identity != null && principal.Identity.IsAuthenticated;
    
    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <param name="principal">The principal.</param>
    /// <returns></returns>
    [DebuggerStepThrough]
    public static string GetDisplayName(this ClaimsPrincipal principal) {
        var name = principal.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(name)) return name;

        var sub = principal.FindFirst(BasicClaimTypes.Subject);
        if (sub != null) return sub.Value;

        return string.Empty;
    }
}
