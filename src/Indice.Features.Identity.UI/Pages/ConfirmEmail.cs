using IdentityServer4.Services;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the confirm email page screen.</summary>
[IdentityUI(typeof(ConfirmEmailModel))]
[SecurityHeaders]
public abstract class BaseConfirmEmailModel : BasePageModel
{
    private readonly IStringLocalizer<BaseConfirmEmailModel> _localizer;

    /// <summary>Creates a new instance of <see cref="BaseConfirmEmailModel"/> class.</summary>
    /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="BaseConfirmEmailModel"/>.</param>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseConfirmEmailModel(
        IStringLocalizer<BaseConfirmEmailModel> localizer,
        ExtendedUserManager<User> userManager,
        IIdentityServerInteractionService interaction
    ) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        Interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }
    /// <summary>Provide services be used by the user interface to communicate with IdentityServer.</summary>
    protected IIdentityServerInteractionService Interaction { get; }

    /// <summary>The URL to return to.</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>Confirm email page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync(
        [FromQuery] string? userId,
        [FromQuery] string? token,
        [FromQuery] string? returnUrl,
        [FromQuery(Name = "client_id")] string? clientId,
        [FromQuery(Name = "twc")] bool throwWhenConfirmed = true,
        [FromQuery(Name = "sr")] bool shouldRedirect = false
    ) {
        ReturnUrl = returnUrl;
        if (string.IsNullOrWhiteSpace(userId)) {
            throw new ArgumentNullException(nameof(userId), "Parameter cannot be null.");
        }
        if (string.IsNullOrWhiteSpace(token)) {
            throw new ArgumentNullException(nameof(token), "Parameter cannot be null.");
        }
        if (!string.IsNullOrWhiteSpace(ReturnUrl) && !string.IsNullOrWhiteSpace(clientId)) {
            ReturnUrl = QueryHelpers.AddQueryString(ReturnUrl, "client_id", clientId);
        }
        var user = await UserManager.FindByIdAsync(userId);
        if (user is null) {
            ModelState.AddModelError(string.Empty, string.Empty);
            return Page();
        }
        var emailConfirmed = await UserManager.IsEmailConfirmedAsync(user);
        if (emailConfirmed && throwWhenConfirmed) {
            ModelState.AddModelError(string.Empty, string.Empty);
            return Page();
        }
        if (!emailConfirmed) {
            var result = await UserManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded) {
                AddModelErrors(result);
                return Page();
            }
        }
        if (shouldRedirect && !string.IsNullOrWhiteSpace(ReturnUrl) && (Interaction.IsValidReturnUrl(returnUrl) || Url.IsLocalUrl(returnUrl))) {
            return Redirect(ReturnUrl);
        }
        return Page();
    }
}

internal class ConfirmEmailModel : BaseConfirmEmailModel
{
    public ConfirmEmailModel(
        IStringLocalizer<ConfirmEmailModel> localizer,
        ExtendedUserManager<User> userManager,
        IIdentityServerInteractionService interaction
    ) : base(localizer, userManager, interaction) { }
}
