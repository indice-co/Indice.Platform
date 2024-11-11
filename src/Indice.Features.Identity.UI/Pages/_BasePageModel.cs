﻿using System.Globalization;
using IdentityModel;
using IdentityServer4.Services;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Base model class for pages containing some common utility methods.</summary>
public abstract class BasePageModel : PageModel
{
    private IdentityUIOptions? _uiOptions;
    private RequestCulture? _requestCulture;
    private IIdentityServerInteractionService? _interactionService;

    /// <summary>Will propagate to body class</summary>
    [ViewData]
    public string BodyCssClass { get; set; } = "identity-page";
    /// <summary>Defines a mechanism for retrieving a service object.</summary>
    protected IServiceProvider ServiceProvider => HttpContext.RequestServices;
    /// <summary>UI Options</summary>
    public IdentityUIOptions UiOptions => _uiOptions ??= ServiceProvider.GetRequiredService<IOptions<IdentityUIOptions>>().Value;
    /// <summary>Request Culture</summary>
    public RequestCulture RequestCulture => _requestCulture ??= Request.HttpContext.Features.Get<IRequestCultureFeature>()!.RequestCulture;
    /// <summary>Provide services be used by the user interface to communicate with IdentityServer.</summary>
    public IIdentityServerInteractionService InteractionService => _interactionService ??= ServiceProvider.GetRequiredService<IIdentityServerInteractionService>();

    /// <summary>Checks if the given return URL is safe for redirection.</summary>
    /// <param name="returnUrl">The URL to validate.</param>
    public bool IsValidReturnUrl(string? returnUrl) {
        if (string.IsNullOrWhiteSpace(returnUrl)) {
            return false;
        }
        return InteractionService.IsValidReturnUrl(returnUrl) || Url.IsLocalUrl(returnUrl) || UiOptions.IsValidReturnUrl(returnUrl);
    }

    /// <summary>Gets the page to redirect based on the <see cref="SignInResult"/>.</summary>
    /// <param name="result">Represents the result of a sign-in operation.</param>
    /// <param name="returnUrl">The return URL.</param>
    public string? GetRedirectUrl(SignInResult result, string? returnUrl = null) {
        string? url = null;
        if (result.RequiresPasswordChange()) {
            url = Url.PageLink("/PasswordExpired", values: new { returnUrl });
        } else if (result.RequiresEmailConfirmation()) {
            url = Url.PageLink("/AddEmail", values: new { returnUrl });
        } else if (result.RequiresPhoneNumberConfirmation()) {
            url = Url.PageLink("/AddPhone", values: new { returnUrl });
        } else if (result.RequiresTwoFactor) {
            url = Url.PageLink("/Mfa", values: new { returnUrl });
        } else if (result.RequiresMfaOnboarding()) {
            url = Url.PageLink("/MfaOnboarding", values: new { returnUrl });
        }
        return url;
    }

    /// <summary>>Gets the page to redirect based on the <see cref="UserState"/>.</summary>
    /// <param name="loginState">The current user state.</param>
    /// <param name="returnUrl">The return URL.</param>
    public string? GetRedirectUrl(UserState loginState, string? returnUrl = null) => loginState switch {
        UserState.LoggedOut or UserState.LoggedIn => IsValidReturnUrl(returnUrl) ? returnUrl : "/",
        UserState.RequiresPhoneNumberVerification => Url.PageLink("/AddPhone", values: new { returnUrl }),
        UserState.RequiresEmailVerification => Url.PageLink("/AddEmail", values: new { returnUrl }),
        UserState.RequiresPasswordChange => Url.PageLink("/PasswordExpired", values: new { returnUrl }),
        UserState.RequiresMfa => Url.PageLink("/Mfa", values: new { returnUrl }),
        UserState.RequiresMfaOnboarding => Url.PageLink("/MfaOnboarding", values: new { returnUrl }),
        _ => default
    };

    /// <summary>Adds errors contained in <see cref="IdentityResult"/> to the <see cref="ModelStateDictionary"/>.</summary>
    /// <param name="result">Represents the result of a sign-in operation.</param>
    public virtual void AddModelErrors(IdentityResult result) {
        if (result.Succeeded) {
            return;
        }
        if (result.Errors?.Count() > 0) {
            foreach (var error in result.Errors) {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }

    /// <summary>Generates a registration email confirmation link and sends it to the email of the specified user.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task SendConfirmationEmail(User user, string? returnUrl = null) {
        var userManager = ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        if (!string.IsNullOrEmpty(returnUrl) && InteractionService.IsValidReturnUrl(returnUrl)) {
            // if this is a login url use it to extract the client_id param but remove it from the email link
            // If a return url such as a login url is baked into the confirmation chanses are that it will
            // 1. either become invild by the time the user clicks his email
            // 2. it will make the confirmation link so big it will fail on multiple browsers.
            // so remove the thing.
            returnUrl = null;
        }
        var callbackUrl = Url.PageLink("/ConfirmEmail", values: new { userId = user.Id, token, returnUrl, client_id = HttpContext.GetClientIdFromReturnUrl() }, protocol: HttpContext.Request.Scheme ?? null);
        var emailService = ServiceProvider.GetRequiredService<IEmailService>();
        var localizer = ServiceProvider.GetRequiredService<IStringLocalizer<BasePageModel>>();
        await emailService.SendAsync(message =>
            message.To(user.Email!)
                   .WithSubject(localizer["Account confirmation"])
                   .UsingTemplate("EmailRegister")
                   .WithData(new {
                       user.UserName,
                       Url = callbackUrl
                   })
        );
        var logger = ServiceProvider.GetRequiredService<ILogger<BasePageModel>>();
        logger.LogInformation("Sending a confirmation email to {Email} with callback URL: {CallbackUrl}.", user.Email, callbackUrl);
    }

    /// <summary>Generates a change email confirmation link and sends it to the email of the specified user.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="newEmail">The new email of the user.</param>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task SendChangeEmailConfirmationEmail(User user, string newEmail, string? returnUrl = null) {
        var userManager = ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var token = await userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var configuration = ServiceProvider.GetRequiredService<IConfiguration>();
        var generalSettings = configuration.GetGeneralSettings();
        if (!string.IsNullOrEmpty(returnUrl) && InteractionService.IsValidReturnUrl(returnUrl)) {
            // if this is a login url use it to extract the client_id param but remove it from the email link
            // If a return url such as a login url is baked into the confirmation chanses are that it will
            // 1. either become invild by the time the user clicks his email
            // 2. it will make the confirmation link so big it will fail on multiple browsers.
            // so remove the thing.
            returnUrl = null;
        }
        var callbackUrl = Url.PageLink("/ChangeEmail", values: new { userId = user.Id, token, email = newEmail, returnUrl, client_id = HttpContext.GetClientIdFromReturnUrl() });
        var claims = await userManager.GetClaimsAsync(user);
        var emailService = ServiceProvider.GetRequiredService<IEmailService>();
        var localizer = ServiceProvider.GetRequiredService<IStringLocalizer<BasePageModel>>();
        await emailService.SendAsync(message =>
            message.To(user.Email!)
                   .WithSubject(localizer["Account confirmation"])
                   .UsingTemplate("EmailConfirmYourEmail")
                   .WithData(new {
                       UserName = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value ?? user.UserName,
                       Url = callbackUrl
                   })
        );
    }

    /// <summary>Generates a TOTP code and sends it to the phone number of the specified user.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="phoneNumber">The phone number.</param>
    public virtual async Task SendVerificationSmsAsync(User user, string phoneNumber) {
        var userManager = ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var code = await userManager.GenerateChangePhoneNumberTokenAsync(user, phoneNumber);
        var smsService = ServiceProvider.GetRequiredService<ISmsService>();
        var localizer = ServiceProvider.GetRequiredService<IStringLocalizer<BasePageModel>>();
        await smsService.SendAsync(phoneNumber, localizer["Verify phone number"], localizer["OTP CODE: {0} FOR PHONE NUMBER VERIFICATION. IT WILL BE VALID FOR 2 MINUTES.", code]);
    }
}
