using Indice.AspNetCore.Filters;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the Terms and Conditions screen.</summary>
[IdentityUI(typeof(TermsModel))]
[SecurityHeaders]
public abstract class BaseTermsModel : BaseArticlePageModel
{
    /// <summary>settings</summary>
    protected IdentityUIOptions Options { get; }
    
    /// <summary>Creates a new instance of <see cref="BaseTermsModel"/> class.</summary>
    /// <param name="options"></param>
    public BaseTermsModel(IOptions<IdentityUIOptions> options) {
        Options = options.Value;
    }

    /// <summary>Render the page</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        if (!string.IsNullOrWhiteSpace(Options.TermsUrl) && Uri.IsWellFormedUriString(Options.TermsUrl, UriKind.Absolute)) {
            return Redirect(Options.TermsUrl);
        }
        return await Article("Terms of Service", "~/legal/terms-of-service.md");
    }
}

internal class TermsModel : BaseTermsModel
{
    public TermsModel(
        IOptions<IdentityUIOptions> options
    ) : base(options) {
    }
}