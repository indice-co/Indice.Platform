using System.Security.Claims;
using IdentityModel;
using IdentityServer4.Services;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity;
using Indice.AspNetCore.Identity.Data.Models;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Identity.Controllers;

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

    /// <summary>Displays the access denied page.</summary>
    [HttpGet("access-denied")]
    public IActionResult AccessDenied() => View();
}
