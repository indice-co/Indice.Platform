using System;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Mvc;

namespace Indice.AspNetCore.Identity.Extensions
{
    /// <summary>Extension methods on <see cref="IClientStore"/>.</summary>
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

        /// <summary>A loading page.</summary>
        /// <param name="controller">A base class for an MVC controller with view support.</param>
        /// <param name="viewName">The name of the view.</param>
        /// <param name="redirectUri">The redirect URI.</param>
        public static IActionResult LoadingPage(this Controller controller, string viewName, string redirectUri) {
            controller.HttpContext.Response.StatusCode = 200;
            controller.HttpContext.Response.Headers["Location"] = string.Empty;
            return controller.View(viewName, new RedirectViewModel { 
                RedirectUrl = redirectUri 
            });
        }
    }
}
