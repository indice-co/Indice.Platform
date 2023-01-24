using System.Security.Claims;
using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Identity.Controllers
{
    /// <summary>Contains all methods related to a user's account.</summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    [SecurityHeaders]
    public class AccountController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly ExtendedUserManager<User> _userManager;
        private readonly ExtendedSignInManager<User> _signInManager;
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountController> _logger;
        /// <summary>The name of the controller.</summary>
        public const string Name = "Account";

        /// <summary>Creates a new instance of <see cref="AccountController"/>.</summary>
        /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
        /// <param name="events">Interface for the event service.</param>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="signInManager">Provides the APIs for user sign in.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        /// <param name="accountService">Wraps account controller operations regarding creating and validating view models.</param>
        public AccountController(
            IIdentityServerInteractionService interaction,
            IEventService events,
            ExtendedUserManager<User> userManager,
            ExtendedSignInManager<User> signInManager,
            ILogger<AccountController> logger,
            IAccountService accountService
        ) {
            _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string UserId => User.FindFirstValue(JwtClaimTypes.Subject);
        public string UserName => User.FindFirstValue(JwtClaimTypes.Name);

        /// <summary>Renders the logout page.</summary>
        /// <param name="logoutId">The logout id.</param>
        [HttpGet("logout")]
        public async Task<IActionResult> Logout(string logoutId) {
            // Build a model so the logout page knows what to display.
            var viewModel = await _accountService.BuildLogoutViewModelAsync(logoutId);
            if (!viewModel.ShowLogoutPrompt) {
                // If the request for logout was properly authenticated from IdentityServer, then we don't need to show the prompt and can just log the user out directly.
                return await Logout(viewModel);
            }
            return View(viewModel);
        }

        /// <summary>Posts logout information to the server.</summary>
        /// <param name="model">The model that contains user's logout info.</param>
        [HttpPost("logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutInputModel model) {
            // Build a model so the logged out page knows what to display.
            var viewModel = await _accountService.BuildLoggedOutViewModelAsync(model.LogoutId);
            if (User?.Identity.IsAuthenticated == true) {
                // Delete local authentication cookies.
                await _signInManager.SignOutAsync();
                // Raise the logout event.
                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
            }
            // Check if we need to trigger sign-out at an upstream identity provider.
            if (viewModel.TriggerExternalSignout) {
                // Build a return URL so the upstream provider will redirect back to us after the user has logged out. this allows us to then complete our single sign-out processing.
                var url = Url.Action(nameof(Logout), new {
                    logoutId = viewModel.LogoutId
                });
                // this triggers a redirect to the external provider for sign-out
                return SignOut(new AuthenticationProperties {
                    RedirectUri = url
                }, viewModel.ExternalAuthenticationScheme);
            }
            // Set this so UI rendering sees an anonymous user.
            return View("LoggedOut", viewModel);
        }

        /// <summary>Displays the access denied page.</summary>
        [HttpGet("access-denied")]
        public IActionResult AccessDenied() => View();
    }
}
