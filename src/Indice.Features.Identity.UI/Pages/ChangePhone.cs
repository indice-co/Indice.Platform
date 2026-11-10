using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Indice.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the change phone number screen. The new number is applied only after it is verified with an OTP.</summary>
[Authorize]
[IdentityUI(typeof(ChangePhoneModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseChangePhoneModel : BasePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseChangePhoneModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="signInManager">Provides the APIs for user sign in.</param>
    /// <param name="identityUiOptions">Configuration options for Identity UI.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseChangePhoneModel(
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        IOptions<IdentityUIOptions> identityUiOptions
    ) : base() {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        SignInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        IdentityUiOptions = identityUiOptions?.Value ?? throw new ArgumentNullException(nameof(identityUiOptions));
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }
    /// <summary>Provides the APIs for user sign in.</summary>
    protected ExtendedSignInManager<User> SignInManager { get; }
    /// <summary>Configuration options for Identity UI.</summary>
    public IdentityUIOptions IdentityUiOptions { get; }

    /// <summary>The input model for the new phone number step.</summary>
    public AddPhoneInputModel Input { get; set; } = new AddPhoneInputModel();
    /// <summary>The input model for the OTP verification step.</summary>
    public VerifyPhoneInputModel Verify { get; set; } = new VerifyPhoneInputModel();
    /// <summary>Indicates that the OTP has been sent and the verification step should be displayed.</summary>
    public bool CodeSent { get; set; }
    /// <summary>The current phone number of the user.</summary>
    public string? CurrentPhoneNumber { get; set; }

    /// <summary>Change phone page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        if (IdentityUiOptions.DisablePhoneEdit) {
            return RedirectToPage("/Profile");
        }
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        CurrentPhoneNumber = user.PhoneNumber;
        if (PhoneNumber.TryParse(user.PhoneNumber!, out var phone)) {
            Input.CallingCode = phone.CallingCode;
            Input.PhoneNumber = phone.Number;
        }
        return Page();
    }

    /// <summary>Change phone page POST handler. Sends an OTP to the new phone number.</summary>
    public virtual async Task<IActionResult> OnPostAsync(AddPhoneInputModel input) {
        if (IdentityUiOptions.DisablePhoneEdit) {
            return BadRequest();
        }
        Input = input;
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        CurrentPhoneNumber = user.PhoneNumber;
        if (!ModelState.IsValid) {
            return Page();
        }
        if (!PhoneNumber.TryParse(Input.PhoneNumberWithCallingCode!, out var phone)) {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.PhoneNumberWithCallingCode)}", UserManager.MessageDescriber.InvalidPhoneNumber);
            return Page();
        }
        var newPhoneNumber = phone.ToString(IdentityUiOptions.PhoneNumberStoreFormat);
        if (user.PhoneNumberConfirmed && newPhoneNumber == user.PhoneNumber) {
            return RedirectToPage("/Profile");
        }
        if (!await SendVerificationSmsAsync(user, newPhoneNumber)) {
            ModelState.AddModelError(string.Empty, UserManager.MessageDescriber.LimitAttemptsReached);
            return Page();
        }
        Verify.PhoneNumber = newPhoneNumber;
        CodeSent = true;
        return Page();
    }

    /// <summary>Change phone page verify POST handler. Applies the new phone number when the OTP is valid, or resends the OTP.</summary>
    public virtual async Task<IActionResult> OnPostVerifyAsync(VerifyPhoneInputModel verify) {
        if (IdentityUiOptions.DisablePhoneEdit) {
            return BadRequest();
        }
        Verify = verify;
        CodeSent = true;
        if (PhoneNumber.TryParse(Verify.PhoneNumber ?? string.Empty, out var pendingPhone)) {
            Input.CallingCode = pendingPhone.CallingCode;
            Input.PhoneNumber = pendingPhone.Number;
        }
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        CurrentPhoneNumber = user.PhoneNumber;
        if (!ModelState.IsValid) {
            return Page();
        }
        if (Verify.OtpResend) {
            ModelState.Clear();
            Verify.OtpResend = false;
            Verify.Code = null;
            if (!await SendVerificationSmsAsync(user, Verify.PhoneNumber!)) {
                ModelState.AddModelError(string.Empty, UserManager.MessageDescriber.LimitAttemptsReached);
            }
            return Page();
        }
        var result = await UserManager.ChangePhoneNumberAsync(user, Verify.PhoneNumber!, Verify.Code!);
        if (!result.Succeeded) {
            ModelState.AddModelError($"{nameof(Verify)}.{nameof(Verify.Code)}", UserManager.MessageDescriber.RegisterPhoneConfirmationFailed);
            return Page();
        }
        await SignInManager.RefreshSignInAsync(user);
        var localizer = HttpContext.RequestServices.GetService(typeof(IdentityUILocalizer)) as IdentityUILocalizer;
        TempData.Put("Alert", AlertModel.Success(localizer?.Profile_Your_Profile_Was_Updated_Successfully.Value ?? string.Empty));
        return RedirectToPage("/Profile");
    }
}

internal class ChangePhoneModel : BaseChangePhoneModel
{
    public ChangePhoneModel(
        ExtendedUserManager<User> userManager,
        ExtendedSignInManager<User> signInManager,
        IOptions<IdentityUIOptions> identityUiOptions
    ) : base(userManager, signInManager, identityUiOptions) { }
}
