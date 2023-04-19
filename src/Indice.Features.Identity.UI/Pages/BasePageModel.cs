using System.Diagnostics;
using IdentityModel;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Base model class for pages containing some common utility methods.</summary>
public abstract class BasePageModel : PageModel
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary></summary>
    /// <param name="serviceProvider"></param>
    public BasePageModel(IServiceProvider serviceProvider) {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary></summary>
    /// <param name="result"></param>
    /// <param name="returnUrl"></param>
    public RedirectToPageResult GetRedirectUrl(Microsoft.AspNetCore.Identity.SignInResult result, string returnUrl = null) {
        RedirectToPageResult redirectResult = null;
        var extendedResult = result as ExtendedSignInResult;
        if (extendedResult?.RequiresPasswordChange() == true) {
            redirectResult = RedirectToPage("/login/password-expired", new { returnUrl });
        } else if (extendedResult?.RequiresEmailConfirmation() == true) {
            redirectResult = RedirectToPage("/login/add-email", new { returnUrl });
        } else if (extendedResult?.RequiresPhoneNumberConfirmation() == true) {
            redirectResult = RedirectToPage("/login/add-phone", new { returnUrl });
        } else if (result.RequiresTwoFactor) {
            redirectResult = RedirectToPage("/login/mfa", new { returnUrl });
        } else if (result.RequiresMfaOnboarding()) {
            redirectResult = RedirectToPage("/login/mfa/onboarding", new { returnUrl });
        }
        return redirectResult;
    }

    /// <summary>Adds errors contained in <see cref="IdentityResult"/> to the <see cref="ModelStateDictionary"/>.</summary>
    /// <param name="result">The identity result.</param>
    public virtual void AddModelErrors(IdentityResult result) {
        if (result.Succeeded) {
            return;
        }
        if (result?.Errors?.Count() > 0) {
            foreach (var error in result.Errors) {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }

    /// <summary></summary>
    /// <param name="user"></param>
    /// <param name="newEmail"></param>
    /// <param name="returnUrl"></param>
    public virtual async Task SendChangeEmailConfirmationEmail(User user, string newEmail, string returnUrl = null) {
        var userManager = _serviceProvider.GetRequiredService<ExtendedUserManager<User>>();
        var token = await userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var configuration = _serviceProvider.GetRequiredService<IConfiguration>();
        var generalSettings = configuration.GetGeneralSettings();
        var callbackUrl = $"{generalSettings.Host}{Url.PageLink("ChangeEmail", values: new { userId = user.Id, token, email = newEmail, returnUrl })}";
        var claims = await userManager.GetClaimsAsync(user);
        var hostingEnvironment = _serviceProvider.GetRequiredService<IWebHostEnvironment>();
        if (!hostingEnvironment.IsDevelopment()) {
            var emailService = _serviceProvider.GetRequiredService<IEmailService>();
            var localizer = _serviceProvider.GetRequiredService<IStringLocalizer<BasePageModel>>();
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
}
