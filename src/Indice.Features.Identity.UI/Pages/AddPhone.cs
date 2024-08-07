using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Indice.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the extended validation add email screen.</summary>
[Authorize(AuthenticationSchemes = ExtendedIdentityConstants.ExtendedValidationUserIdScheme)]
[IdentityUI(typeof(AddPhoneModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseAddPhoneModel : BasePageModel
{
    private readonly IStringLocalizer<BaseAddPhoneModel> _localizer;

    /// <summary>Creates a new instance of <see cref="BaseAddEmailModel"/> class.</summary>
    /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="BaseAddEmailModel"/>.</param>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="identityUiOptions">Configuration options for Identity UI.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseAddPhoneModel(
        IStringLocalizer<BaseAddPhoneModel> localizer,
        ExtendedUserManager<User> userManager,
        IOptions<IdentityUIOptions> identityUiOptions
    ) : base() {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        IdentityUiOptions = identityUiOptions?.Value ?? throw new ArgumentNullException(nameof(identityUiOptions));
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }

    /// <summary>Configuration options for Identity UI.</summary>
    public IdentityUIOptions IdentityUiOptions { get; }

    /// <summary>The input model that backs the add phone page.</summary>
    [BindProperty]
    public AddPhoneInputModel Input { get; set; } = new AddPhoneInputModel();

    /// <summary>Key used for setting and retrieving temp data.</summary>
    public static string TempDataKey => "info_message";

    /// <summary>Extended validation add phone page GET handler.</summary>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task<IActionResult> OnGetAsync([FromQuery] string? returnUrl) {
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        if (UserManager.StateProvider.CurrentState != UserState.RequiresPhoneNumberVerification) {
            var redirectUrl = GetRedirectUrl(UserManager.StateProvider.CurrentState, returnUrl);
            return Redirect(redirectUrl ?? "/");
        }
        TempData.Put(TempDataKey, new AlertModel {
            Message = _localizer["Please select your phone number so we can verify it before we continue."],
            AlertType = AlertType.Info
        });
        _ = PhoneNumber.TryParse(user.PhoneNumber!, out var phone);
        Input.PhoneNumber = phone.Number;
        Input.CallingCode = phone.CallingCode;
        Input.ReturnUrl = returnUrl;
        return Page();
    }

    /// <summary>Extended validation add phone page POST handler.</summary>
    public virtual async Task<IActionResult> OnPostAsync([FromQuery] string? returnUrl) {
        if (!ModelState.IsValid) {
            return Page();
        }
        if (!PhoneNumber.TryParse(Input.PhoneNumberWithCallingCode!, out var phone)) {
            ModelState.AddModelError(nameof(Input.PhoneNumber), "Phone number is not valid.");
            return Page();
        }
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        var result = await UserManager.SetPhoneNumberAsync(user, phone.ToString(IdentityUiOptions.PhoneNumberStoreFormat));
        if (!result.Succeeded) {
            AddModelErrors(result);
            return Page();
        }
        await SendVerificationSmsAsync(user, phone.ToString(IdentityUiOptions.PhoneNumberStoreFormat));
        return RedirectToPage("/VerifyPhone", new { returnUrl });
    }
}

internal class AddPhoneModel : BaseAddPhoneModel
{
    public AddPhoneModel(
        IStringLocalizer<AddPhoneModel> localizer,
        ExtendedUserManager<User> userManager,
        IOptions<IdentityUIOptions> identityUiOptions
    ) : base(localizer, userManager, identityUiOptions) { }
}
