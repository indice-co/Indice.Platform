using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

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
    public static IActionResult LoadingPage(this PageModel page, string pageName, string redirectUri) {
        page.HttpContext.Response.StatusCode = 200;
        page.HttpContext.Response.Headers["Location"] = string.Empty;
        return page.RedirectToPage(pageName, new { RedirectUri = redirectUri });
    }

    /// <summary>Determines if the authentication scheme support sign out.</summary>
    public static async Task<bool> GetSchemeSupportsSignOutAsync(this HttpContext context, string scheme) {
        var provider = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
        var handler = await provider.GetHandlerAsync(context, scheme);
        return handler is IAuthenticationSignOutHandler;
    }
}
