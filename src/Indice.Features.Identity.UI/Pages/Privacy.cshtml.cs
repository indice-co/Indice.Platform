using Indice.AspNetCore.Filters;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the privacy screen.</summary>
[IdentityUI(typeof(PrivacyModel))]
[SecurityHeaders]
public abstract class BasePrivacyModel : BaseArticlePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseTermsModel"/> class.</summary>
    /// <param name="options"></param>
    public BasePrivacyModel(IOptions<IdentityUIOptions> options) {
        Options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Identity UI options.</summary>
    protected IdentityUIOptions Options { get; }

    /// <summary>Privacy page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        if (!string.IsNullOrWhiteSpace(Options.PrivacyUrl) && Uri.IsWellFormedUriString(Options.PrivacyUrl, UriKind.Absolute)) {
            return Redirect(Options.PrivacyUrl);
        }
        return await Article("Privacy Policy", "~/legal/privacy-policy.md");
    }
}

internal class PrivacyModel : BasePrivacyModel
{
    public PrivacyModel(IOptions<IdentityUIOptions> options) : base(options) { }
}