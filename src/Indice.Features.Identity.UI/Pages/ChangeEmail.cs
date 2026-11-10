using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Guards;
using Indice.Features.Identity.UI.Models;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the change email screen.</summary>
[Authorize]
[IdentityUI(typeof(ChangeEmailModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseChangeEmailModel : BasePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseChangeEmailModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="identityUiOptions">Configuration options for Identity UI.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseChangeEmailModel(
        ExtendedUserManager<User> userManager,
        IOptions<IdentityUIOptions> identityUiOptions
    ) : base() {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        IdentityUiOptions = identityUiOptions?.Value ?? throw new ArgumentNullException(nameof(identityUiOptions));
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }
    /// <summary>Configuration options for Identity UI.</summary>
    public IdentityUIOptions IdentityUiOptions { get; }

    /// <summary>The input model that backs the change email page.</summary>
    [BindProperty]
    public AddEmailInputModel Input { get; set; } = new AddEmailInputModel();

    /// <summary>Indicates that the user consents to the processing of personal data.</summary>
    [BindProperty]
    public bool PrivacyConsent { get; set; }

    /// <summary>The one-time code entered by the user when <see cref="EmailConfirmationMethod.Otp"/> is configured.</summary>
    [BindProperty]
    public string? Code { get; set; }

    /// <summary>The current email of the user.</summary>
    public string? CurrentEmail { get; set; }

    /// <summary>The new email awaiting confirmation. When set, the confirmation dialog is displayed.</summary>
    public string? PendingEmail { get; set; }

    /// <summary>Indicates whether the confirmation is done using a one-time code.</summary>
    public bool UseOtp => IdentityUiOptions.EmailConfirmationMethod == EmailConfirmationMethod.Otp;

    /// <summary>The token purpose used to bind an email change OTP to the new email address.</summary>
    protected static string ChangeEmailOtpPurpose(string newEmail) => $"ChangeEmailOtp:{newEmail.ToUpperInvariant()}";

    /// <summary>Change email page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        if (IdentityUiOptions.DisableEmailEdit) {
            return RedirectToPage("/Profile");
        }
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        CurrentEmail = user.Email;
        return Page();
    }

    /// <summary>Change email page POST handler. Sends a confirmation link or a one-time code to the new address.</summary>
    public virtual async Task<IActionResult> OnPostAsync() {
        if (IdentityUiOptions.DisableEmailEdit) {
            return BadRequest();
        }
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        CurrentEmail = user.Email;
        ModelState.Remove(nameof(Code));
        if (!PrivacyConsent) {
            ModelState.AddModelError(nameof(PrivacyConsent), IdentityLabels.ChangeEmail_ConsentRequired);
        }
        if (!ModelState.IsValid) {
            return Page();
        }
        var newEmail = Input.Email!.Trim();
        if (string.Equals(user.NormalizedEmail, newEmail.ToUpperInvariant(), StringComparison.Ordinal)) {
            return RedirectToPage("/Profile");
        }
        var sent = UseOtp ? await SendChangeEmailOtp(user, newEmail) : await SendChangeEmailConfirmationEmail(user, newEmail);
        if (!sent) {
            ModelState.AddModelError(string.Empty, UserManager.MessageDescriber.LimitAttemptsReached);
            return Page();
        }
        PendingEmail = newEmail;
        return Page();
    }

    /// <summary>Verifies the one-time code sent to the new email and applies the change, or resends the code.</summary>
    /// <param name="otpResend">Indicates that a new code should be sent.</param>
    public virtual async Task<IActionResult> OnPostVerifyAsync([FromForm] bool otpResend = false) {
        if (IdentityUiOptions.DisableEmailEdit || !UseOtp) {
            return BadRequest();
        }
        var user = await UserManager.GetUserAsync(User) ?? throw new InvalidOperationException("User cannot be null.");
        CurrentEmail = user.Email;
        ModelState.Clear();
        var newEmail = Input.Email?.Trim();
        if (string.IsNullOrWhiteSpace(newEmail)) {
            return RedirectToPage();
        }
        PendingEmail = newEmail;
        PrivacyConsent = true;
        if (otpResend) {
            if (!await SendChangeEmailOtp(user, newEmail)) {
                ModelState.AddModelError(nameof(Code), UserManager.MessageDescriber.LimitAttemptsReached);
            }
            return Page();
        }
        var code = Code?.Trim();
        if (string.IsNullOrEmpty(code) || !await UserManager.VerifyUserTokenAsync(user, TokenOptions.DefaultEmailProvider, ChangeEmailOtpPurpose(newEmail), code)) {
            ModelState.AddModelError(nameof(Code), UserManager.ErrorDescriber.InvalidToken().Description);
            return Page();
        }
        var token = await UserManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var result = await UserManager.ChangeEmailAsync(user, newEmail, token);
        if (!result.Succeeded) {
            foreach (var error in result.Errors) {
                ModelState.AddModelError(nameof(Code), error.Description);
            }
            return Page();
        }
        var signInManager = HttpContext.RequestServices.GetRequiredService<ExtendedSignInManager<User>>();
        await signInManager.RefreshSignInAsync(user);
        TempData.Put("Alert", AlertModel.Success(IdentityLabels.ChangeEmail_Succeeded));
        return RedirectToPage("/Profile");
    }

    /// <summary>Generates a one-time code bound to <paramref name="newEmail"/> and sends it to the new address.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="newEmail">The new email address.</param>
    /// <returns>False if the attempts limit has been reached, otherwise true.</returns>
    protected virtual async Task<bool> SendChangeEmailOtp(User user, string newEmail) {
        var userActionGuard = HttpContext.RequestServices.GetRequiredService<IActionRateLimiter>();
        if (!await userActionGuard.CheckAndAdvanceAsync(user.Id, "Email:ChangeEmail")) {
            return false;
        }
        var code = await UserManager.GenerateUserTokenAsync(user, TokenOptions.DefaultEmailProvider, ChangeEmailOtpPurpose(newEmail));
        var emailService = HttpContext.RequestServices.GetRequiredService<IEmailService>();
        var identityMessageDescriber = HttpContext.RequestServices.GetRequiredService<IdentityMessageDescriber>();
        await emailService.SendAsync(message =>
            message.To(newEmail)
                   .WithSubject(identityMessageDescriber.ConfirmationEmailChangeSubject)
                   .UsingTemplate("EmailConfirmYourEmailOtp")
                   .WithData(new {
                       user.UserName,
                       RecipientEmail = newEmail,
                       Subject = identityMessageDescriber.ConfirmationEmailChangeSubject,
                       Code = code
                   })
        );
        return true;
    }
}

internal class ChangeEmailModel : BaseChangeEmailModel
{
    public ChangeEmailModel(
        ExtendedUserManager<User> userManager,
        IOptions<IdentityUIOptions> identityUiOptions
    ) : base(userManager, identityUiOptions) { }
}
