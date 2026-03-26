using Indice.AspNetCore.Features.Recaptcha;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Extensions;
using Indice.Features.Identity.Core.Models;
using Indice.Features.Identity.UI.Models;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the forgot password screen.</summary>
[AllowAnonymous]
[IdentityUI(typeof(ForgotPasswordModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseForgotPasswordModel : BasePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseForgotPasswordModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <param name="emailService">Abstraction for sending email through different providers and implementations. SMTP, SparkPost, Mailchimp etc.</param>
    /// <param name="recaptchaService">Service for validating reCAPTCHA tokens.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseForgotPasswordModel(
        ExtendedUserManager<User> userManager,
        ILogger<BaseForgotPasswordModel> logger,
        IEmailService emailService,
        IRecaptchaService recaptchaService
    ) {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        EmailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        RecaptchaService = recaptchaService ?? throw new ArgumentNullException(nameof(recaptchaService));
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }
    /// <summary>Represents a type used to perform logging.</summary>
    protected ILogger<BaseForgotPasswordModel> Logger { get; }
    /// <summary>Abstraction for sending email through different providers and implementations. SMTP, SparkPost, Mailchimp etc.</summary>
    protected IEmailService EmailService { get; }
    /// <summary>Service for validating reCAPTCHA tokens.</summary>
    protected IRecaptchaService RecaptchaService { get; }

    /// <summary>Forgot password input model data.</summary>
    [BindProperty]
    public ForgotPasswordInputModel Input { get; set; } = new ForgotPasswordInputModel();

    /// <summary>Determines whether the request is sent once.</summary>
    [ViewData]
    public bool RequestSent { get; set; }

    /// <summary>Forgot password page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        if (!UiOptions.EnableForgotPasswordPage) {
            return Redirect("/404");
        }
        await Task.CompletedTask;
        return Page();
    }

    /// <summary>Forgot password page POST handler.</summary>
    public virtual async Task<IActionResult> OnPostAsync() {
        if (!UiOptions.EnableForgotPasswordPage) {
            return Redirect("/404");
        }

        // Validate reCAPTCHA if enabled
        // Note: For v3, token is pre-validated via /RecaptchaValidate endpoint to check score before form submission.
        //       For v2, this is the first and only validation (v2 is shown when v3 score < threshold).
        if (RecaptchaService.IsEnabled && Input.RecaptchaVersion == "v2" && !string.IsNullOrWhiteSpace(Input.RecaptchaToken)) {
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var recaptchaResult = await RecaptchaService.ValidateAsync(Input.RecaptchaToken, Input.RecaptchaVersion, remoteIp);

            if (!recaptchaResult.Success) {
                Logger.LogWarning("reCAPTCHA validation failed for forgot password request.");
                ModelState.AddModelError(string.Empty, "reCAPTCHA validation failed. Please try again.");
                return Page();
            }
        }

        RequestSent = true;
        if (!ModelState.IsValid) {
            return Page();
        }
        var user = await UserManager.FindByEmailAsync(Input.Email ?? throw new InvalidOperationException("Email cannot be null."));
        if (user is null) {
            return Page();
        }
        if (user.Claims.Count is 0) {
            _ = await UserManager.GetClaimsAsync(user); // Lazy load claims if not already loaded
        }
        var token = await UserManager.GeneratePasswordResetTokenAsync(user);
        var callbackUrl = Url.PageLink("/ForgotPasswordConfirmation", values: new { email = user.Email, token, client_id = HttpContext.GetClientIdFromReturnUrl() });
        if (string.IsNullOrWhiteSpace(callbackUrl)) {
            Logger.LogError("Failed to generate callback URL for forgot password confirmation email for user: {userId}.", user.Id);
            return Page();
        }
        var maskedToken = token.Length > 4 ? string.Concat(token.AsSpan(0, 2), new string('*', token.Length - 4), token.AsSpan(token.Length - 2)) : token;
        Logger.LogDebug("{PageTitle}: Confirmation token is {Token}", "Forgot password", maskedToken);
        await EmailService.SendAsync(builder =>
            builder.To(user.Email!)
                   .WithSubject(UserManager.MessageDescriber.ForgotPasswordMessageSubject)
                   .UsingTemplate("EmailForgotPassword")
                   .WithData(new ForgotPasswordEmailModel {
                       UserName = user.FindDisplayName() ?? user.UserName!,
                       Url = callbackUrl
                   })
        );
        return Page();
    }
}

internal class ForgotPasswordModel : BaseForgotPasswordModel
{
    public ForgotPasswordModel(
        ExtendedUserManager<User> userManager,
        ILogger<ForgotPasswordModel> logger,
        IEmailService emailService,
        IRecaptchaService recaptchaService
    ) : base(userManager, logger, emailService, recaptchaService) { }
}
