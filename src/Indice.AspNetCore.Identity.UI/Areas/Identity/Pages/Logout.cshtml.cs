using System.Security.Claims;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.AspNetCore.Identity.Models;
using Indice.AspNetCore.Identity.UI.Areas.Identity.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Indice.AspNetCore.Identity.UI.Areas.Identity.Pages
{
    /// <summary>Page model for the logout screen.</summary>
    [Authorize]
    [SecurityHeaders]
    public class LogoutModel : PageModel
    {
        private readonly ExtendedSignInManager<User> _signInManager;
        private readonly IEventService _events;
        private readonly IIdentityServerInteractionService _interaction;

        /// <summary>Creates a new instance of <see cref="LogoutModel"/> class.</summary>
        /// <param name="signInManager">Provides the APIs for user sign in.</param>
        /// <param name="events">Interface for the event service.</param>
        /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public LogoutModel(
            ExtendedSignInManager<User> signInManager,
            IEventService events,
            IIdentityServerInteractionService interaction
        ) {
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
        }

        /// <summary>Login input model data.</summary>
        [BindProperty]
        public LogoutInputModelTemp Input { get; set; }

        /// <summary>Should show the prompt or auto logout?</summary>
        public bool ShowLogoutPrompt { get; set; } = true;

        /// <summary>Logout page GET handler.</summary>
        public async Task<IActionResult> OnGetAsync(string logoutId) {
            await BuildLogoutModelAsync(logoutId);
            if (ShowLogoutPrompt == false) {
                //return await OnPostAsync();
            }
            return Page();
        }

        /// <summary>Logout page POST handler.</summary>
        public void OnPostAsync() {
        }

        private async Task BuildLogoutModelAsync(string logoutId) {
            Input.LogoutId = logoutId;
            ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt;
            var context = await _interaction.GetLogoutContextAsync(logoutId);
            Input.ClientId = context?.ClientId;
            if (context?.ShowSignoutPrompt == false) {
                ShowLogoutPrompt = false;
            }
        }
    }
}
