using Indice.AspNetCore.Filters;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the Privacy screen.</summary>
[IdentityUI(typeof(PrivacyModel))]
[SecurityHeaders]
public abstract class BasePrivacyModel : BaseArticlePageModel
{
    /// <summary>settings</summary>
    protected IdentityUIOptions Options { get; }

    /// <summary>Creates a new instance of <see cref="BaseTermsModel"/> class.</summary>
    /// <param name="options"></param>
    public BasePrivacyModel(IOptions<IdentityUIOptions> options) {
        Options = options.Value;
    }

    /// <summary>Render the page</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        if (!string.IsNullOrWhiteSpace(Options.PrivacyUrl) && Uri.IsWellFormedUriString(Options.PrivacyUrl, UriKind.Absolute)) {
            return Redirect(Options.PrivacyUrl);
        }
        return await Article("Privacy Policy", "~/legal/privacy-policy.md");
    }
}

internal class PrivacyModel : BasePrivacyModel
{
    public PrivacyModel(
        IOptions<IdentityUIOptions> options
    ) : base(options) {
    }
}