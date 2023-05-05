using System.Diagnostics;
using IdentityModel;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Base model class for pages containing some common utility methods.</summary>
public abstract class BasePageModel : PageModel
{
    /// <summary>Defines a mechanism for retrieving a service object.</summary>
    protected IServiceProvider ServiceProvider => HttpContext.RequestServices;

    /// <summary>Gets the page to redirect based on the <see cref="SignInResult"/>.</summary>
    /// <param name="result">Represents the result of a sign-in operation.</param>
    /// <param name="returnUrl">The return URL.</param>
    public RedirectToPageResult? GetRedirectToPageResult(SignInResult result, string? returnUrl = null) {
        RedirectToPageResult? redirectResult = null;
        var extendedResult = result as ExtendedSignInResult;
        if (extendedResult?.RequiresPasswordChange() == true) {
            redirectResult = RedirectToPage("PasswordExpired", new { returnUrl });
        } else if (extendedResult?.RequiresEmailConfirmation() == true) {
            redirectResult = RedirectToPage("AddEmail", new { returnUrl });
        } else if (extendedResult?.RequiresPhoneNumberConfirmation() == true) {
            redirectResult = RedirectToPage("AddPhone", new { returnUrl });
        } else if (result.RequiresTwoFactor) {
            redirectResult = RedirectToPage("Mfa", new { returnUrl });
        } else if (result.RequiresMfaOnboarding()) {
            redirectResult = RedirectToPage("MfaOnboarding", new { returnUrl });
        }
        return redirectResult;
    }

    /// <summary>>Gets the page to redirect based on the <see cref="UserState"/>.</summary>
    /// <param name="loginState">The current user state.</param>
    /// <param name="returnUrl">The return URL.</param>
    public string? GetRedirectUrl(UserState loginState, string? returnUrl = null) => loginState switch {
        UserState.LoggedOut or UserState.LoggedIn => "/",
        UserState.RequiresPhoneNumberVerification => Url.PageLink("AddPhone", values: new { returnUrl }),
        UserState.RequiresEmailVerification => Url.PageLink("AddEmail", values: new { returnUrl }),
        UserState.RequiresPasswordChange => Url.PageLink("PasswordExpired", values: new { returnUrl }),
        UserState.RequiresMfa => Url.PageLink("Mfa", values: new { returnUrl }),
        UserState.RequiresMfaOnboarding => Url.PageLink("MfaOnboarding", values: new { returnUrl }),
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

    /// <summary>Sends an email, using the 'EmailRegister' page as template, containing a verification link for user's email.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="returnUrl">The return URL.</param>
    public virtual async Task SendConfirmationEmail(User user, string? returnUrl = null) {
        var userManager = ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var callbackUrl = Url.PageLink("ConfirmEmail", values: new { userId = user.Id, token, returnUrl }, protocol: HttpContext.Request.Scheme ?? null);
        var hostingEnvironment = ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        if (!hostingEnvironment.IsDevelopment()) {
            var emailService = ServiceProvider.GetRequiredService<IEmailService>();
            await emailService.SendAsync(message =>
                message.To(user.Email)
                       .WithSubject("Account confirmation")
                       .UsingTemplate("EmailRegister")
                       .WithData(new {
                           user.UserName,
                           Url = callbackUrl
                       })
            );
            var logger = ServiceProvider.GetRequiredService<ILogger<BasePageModel>>();
            logger.LogInformation("Sending a confirmation email to {Email} with callback URL: {CallbackUrl}.", user.Email, callbackUrl);
        } else {
            Debug.WriteLine($"Link to confirm account: {callbackUrl}");
        }
    }

    /// <summary></summary>
    /// <param name="user"></param>
    /// <param name="newEmail"></param>
    /// <param name="returnUrl"></param>
    public virtual async Task SendChangeEmailConfirmationEmail(User user, string newEmail, string? returnUrl = null) {
        var userManager = ServiceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var token = await userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var configuration = ServiceProvider.GetRequiredService<IConfiguration>();
        var generalSettings = configuration.GetGeneralSettings();
        var callbackUrl = $"{generalSettings.Host}{Url.PageLink("ChangeEmail", values: new { userId = user.Id, token, email = newEmail, returnUrl })}";
        var claims = await userManager.GetClaimsAsync(user);
        var hostingEnvironment = ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        if (!hostingEnvironment.IsDevelopment()) {
            var emailService = ServiceProvider.GetRequiredService<IEmailService>();
            var localizer = ServiceProvider.GetRequiredService<IStringLocalizer<BasePageModel>>();
            await emailService.SendAsync(message =>
                message.To(user.Email)
                       .WithSubject(localizer["Account confirmation"])
                       .UsingTemplate("EmailRegister")
                       .WithData(new {
                           UserName = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value ?? user.UserName,
                           Url = callbackUrl
                       })
            );
        } else {
            Debug.WriteLine($"Link to confirm account: {callbackUrl}");
        }
    }

    /// <summary></summary>
    /// <param name="user"></param>
    /// <param name="scheme"></param>
    public async Task<AuthenticationProperties?> AutoSignIn(User user, string scheme) {
        var authenticateResult = await HttpContext.AuthenticateAsync(scheme);
        AuthenticationProperties? authenticationProperties = default;
        if (authenticateResult.Succeeded) {
            authenticationProperties = authenticateResult.Properties;
            var signInManager = ServiceProvider.GetRequiredService<ExtendedSignInManager<User>>();
            await signInManager.SignInAsync(user, isPersistent: authenticationProperties.IsPersistent);
        }
        return authenticationProperties;
    }
}
