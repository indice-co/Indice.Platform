using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the logout screen.</summary>
[Authorize]
[SecurityHeaders]
public class LogoutPageModel : PageModel
{
    private readonly ExtendedSignInManager<User> _signInManager;
    private readonly IEventService _events;
    private readonly IIdentityServerInteractionService _interaction;

    /// <summary>Creates a new instance of <see cref="LogoutPageModel"/> class.</summary>
    /// <param name="signInManager">Provides the APIs for user sign in.</param>
    /// <param name="events">Interface for the event service.</param>
    /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public LogoutPageModel(
        ExtendedSignInManager<User> signInManager,
        IEventService events,
        IIdentityServerInteractionService interaction
    ) {
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
    }

    /// <summary>The logout id.</summary>
    [BindProperty]
    public string LogoutId { get; set; }
    /// <summary>The id of the current client in the request.</summary>
    [BindProperty]
    public string ClientId { get; set; }
    /// <summary>Should show the prompt or auto logout?</summary>
    public bool ShowLogoutPrompt { get; set; } = true;

    /// <summary>Logout page GET handler.</summary>
    public async Task<IActionResult> OnGetAsync(string logoutId) {
        LogoutId = logoutId;
        var showLogoutPrompt = AccountOptions.ShowLogoutPrompt;
        if (User?.Identity.IsAuthenticated != true) {
            // if the user is not authenticated, then just show logged out page.
            showLogoutPrompt = false;
        } else {
            var context = await _interaction.GetLogoutContextAsync(LogoutId);
            ClientId = context?.ClientId;
            if (context?.ShowSignoutPrompt == false) {
                // It's safe to automatically sign-out.
                showLogoutPrompt = false;
            }
        }
        if (showLogoutPrompt == false) {
            // If the request for logout was properly authenticated from IdentityServer, then we don't need to show the prompt and can just log the user out directly.
            return await OnPostAsync();
        }
        return Page();
    }

    /// <summary>Logout page POST handler.</summary>
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync() {
        if (User?.Identity.IsAuthenticated == true) {
            // If there's no current logout context, we need to create one this captures necessary info from the current logged in user. This can still return null if there is no context needed.
            LogoutId ??= await _interaction.CreateLogoutContextAsync();
            // Delete local authentication cookie.
            await _signInManager.SignOutAsync();
            // Raise the logout event.
            await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
            // See if we need to trigger federated logout.
            var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
            // If it's a local login we can ignore this workflow.
            if (idp != null && idp != IdentityServerConstants.LocalIdentityProvider) {
                // We need to see if the provider supports external logout.
                if (await HttpContext.GetSchemeSupportsSignOutAsync(idp)) {
                    // Build a return URL so the upstream provider will redirect back to us after the user has logged out. This allows us to then complete our single sign-out processing.
                    var url = Url.Page("LoggedOut", new { logoutId = LogoutId });
                    // This triggers a redirect to the external provider for sign-out.
                    return SignOut(new AuthenticationProperties { RedirectUri = url }, idp);
                }
            }
        }
        return RedirectToPage("LoggedOut", new { logoutId = LogoutId });
    }
}
