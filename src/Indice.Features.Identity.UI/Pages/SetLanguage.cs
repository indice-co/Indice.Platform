using System.Globalization;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the set language screen.</summary>
[IdentityUI(typeof(SetLanguageModel))]
[SecurityHeaders]
[IgnoreAntiforgeryToken]
public abstract class BaseSetLanguageModel : BasePageModel
{
    private readonly IdentityCookieRequestCultureProvider _cultureProvider;

    /// <summary>Creates a new instance of <see cref="BaseSetLanguageModel"/> class.</summary>
    public BaseSetLanguageModel(IOptions<RequestLocalizationOptions> requestLocalizationOptions) : base() {
        var options = requestLocalizationOptions.Value;
        _cultureProvider = options.RequestCultureProviders.OfType<IdentityCookieRequestCultureProvider>().FirstOrDefault() ?? new IdentityCookieRequestCultureProvider();
    }

    /// <summary>The submitted culture to change.</summary>
    [BindProperty]
    public string? Culture { get; set; }

    /// <summary>Set language page POST handler.</summary>
    public virtual IActionResult OnPost(string? returnUrl, string? culture) => OnSetLangageInternal(returnUrl, Culture ?? culture);

    /// <summary>Set language page GET handler.</summary>
    public virtual IActionResult OnGet(string? returnUrl, string? culture) => OnSetLangageInternal(returnUrl, culture);


    /// <summary>Set language page POST handler.</summary>
    private IActionResult OnSetLangageInternal(string? returnUrl, string? culture) {
        _cultureProvider.SetLanguage(HttpContext, culture);
        return LocalRedirect(returnUrl ?? "/");
    }
}

internal class SetLanguageModel : BaseSetLanguageModel
{
    public SetLanguageModel(IOptions<RequestLocalizationOptions> requestLocalizationOptions) : base(requestLocalizationOptions) { }
}
