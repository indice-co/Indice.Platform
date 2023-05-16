using Indice.AspNetCore.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the privacy screen.</summary>
[IdentityUI(typeof(PrivacyModel))]
[SecurityHeaders]
public abstract class BasePrivacyModel : BaseArticlePageModel
{
    /// <summary>Privacy page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        if (!string.IsNullOrWhiteSpace(UiOptions.PrivacyUrl) && Uri.IsWellFormedUriString(UiOptions.PrivacyUrl, UriKind.Absolute)) {
            return Redirect(UiOptions.PrivacyUrl);
        }
        return await Article("Privacy Policy", "~/legal/privacy-policy.md");
    }
}

internal class PrivacyModel : BasePrivacyModel
{
    public PrivacyModel() : base() { }
}
