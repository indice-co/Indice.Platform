using Indice.AspNetCore.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the terms and conditions screen.</summary>
[IdentityUI(typeof(TermsModel))]
[SecurityHeaders]
public abstract class BaseTermsModel : BaseArticlePageModel
{
    /// <summary>Terms and conditions page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        if (!string.IsNullOrWhiteSpace(UiOptions.TermsUrl) && Uri.IsWellFormedUriString(UiOptions.TermsUrl, UriKind.Absolute)) {
            return Redirect(UiOptions.TermsUrl);
        }
        return await Article("Terms of Service", "~/legal/terms-of-service.md");
    }
}

internal class TermsModel : BaseTermsModel
{
    public TermsModel() : base() { }
}