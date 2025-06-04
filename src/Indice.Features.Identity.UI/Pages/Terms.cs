using System.Net.Mime;
using System.Text;
using HtmlAgilityPack;
using Indice.AspNetCore.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the terms and conditions screen.</summary>
[IdentityUI(typeof(TermsModel))]
[SecurityHeaders]
public abstract class BaseTermsModel : BaseArticlePageModel
{
    /// <summary>Request raw html without the layout in order to host under a different app.</summary>
    [BindProperty(SupportsGet = true)]
    public bool? Raw { get; set; }

    /// <summary>Terms and conditions page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        if (!string.IsNullOrWhiteSpace(UiOptions.TermsUrl) && Uri.IsWellFormedUriString(UiOptions.TermsUrl, UriKind.Absolute)) {
            return await ExternalArticle(UiOptions.TermsUrl, Raw);
        }
        return await Article("Terms of Service", "~/legal/terms-of-service.md", Raw);
    }


    
}

internal class TermsModel : BaseTermsModel
{
    public TermsModel() : base() { }
}