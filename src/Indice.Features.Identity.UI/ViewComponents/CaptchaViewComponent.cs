using Indice.AspNetCore.Features.Recaptcha;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.ViewComponents;

/// <summary>Renders captcha markup based on configured provider and active UI framework.</summary>
[ViewComponent(Name = "Captcha")]
public class CaptchaViewComponent : ViewComponent
{
    private readonly IRecaptchaService _recaptchaService;

    /// <summary>Creates a new instance of <see cref="CaptchaViewComponent"/>.</summary>
    public CaptchaViewComponent(IRecaptchaService recaptchaService) {
        _recaptchaService = recaptchaService ?? throw new ArgumentNullException(nameof(recaptchaService));
    }

    /// <summary>Renders captcha markup for a target form submit flow.</summary>
    public IViewComponentResult Invoke(string formId, string buttonId, string action, bool isLoginForm = false) {
        var showInForm = (isLoginForm && _recaptchaService.IsEnabledInLogin) || !isLoginForm;
        if (!_recaptchaService.IsEnabled || !showInForm) {
            return Content(string.Empty);
        }

        var model = new RecaptchaViewModel {
            FormId = formId,
            ButtonId = buttonId,
            Action = action,
            IsLoginForm = isLoginForm
        };

        var viewPath = _recaptchaService.Provider switch {
            CaptchaProviderType.Recaptcha => "Recaptcha",
            CaptchaProviderType.HCaptcha => "HCaptcha",
            _ => "DefaultCaptcha"
        };

        return View(viewPath, model);
    }
}
