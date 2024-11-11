using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the forgot password screen.</summary>
[AllowAnonymous]
[IdentityUI(typeof(ForgotPasswordModel))]
[SecurityHeaders]
[ValidateAntiForgeryToken]
public abstract class BaseForgotPasswordModel : BasePageModel
{
    private readonly IStringLocalizer<BaseForgotPasswordModel> _localizer;

    /// <summary>Creates a new instance of <see cref="BaseForgotPasswordModel"/> class.</summary>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <param name="emailService">Abstraction for sending email through different providers and implementations. SMTP, SparkPost, Mailchimp etc.</param>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseForgotPasswordModel(
        ExtendedUserManager<User> userManager,
        ILogger<BaseForgotPasswordModel> logger,
        IEmailService emailService,
        IStringLocalizer<BaseForgotPasswordModel> localizer
    ) {
        UserManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        EmailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>Provides the APIs for managing users and their related data in a persistence store.</summary>
    protected ExtendedUserManager<User> UserManager { get; }
    /// <summary>Represents a type used to perform logging.</summary>
    protected ILogger<BaseForgotPasswordModel> Logger { get; }
    /// <summary>Abstraction for sending email through different providers and implementations. SMTP, SparkPost, Mailchimp etc.</summary>
    protected IEmailService EmailService { get; }

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
        RequestSent = true;
        if (!ModelState.IsValid) {
            return Page();
        }
        var user = await UserManager.FindByEmailAsync(Input.Email ?? throw new InvalidOperationException("Email cannot be null."));
        if (user is null) {
            return Page();
        }
        var token = await UserManager.GeneratePasswordResetTokenAsync(user);
        var callbackUrl = Url.PageLink("/ForgotPasswordConfirmation", values: new { email = user.Email, token, client_id = HttpContext.GetClientIdFromReturnUrl() });
        Logger.LogDebug("{PageTitle}: Confirmation token is {Token}", "Forgot password", token);
        await EmailService.SendAsync(builder =>
            builder.To(user.Email!)
                   .WithSubject(_localizer["Please confirm your account"])
                   .UsingTemplate("EmailForgotPassword")
                   .WithData(new {
                       UserName = User.FindDisplayName() ?? user.UserName,
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
        IStringLocalizer<ForgotPasswordModel> localizer
    ) : base(userManager, logger, emailService, localizer) { }
}
