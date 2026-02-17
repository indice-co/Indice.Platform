using System.Globalization;
using Indice.AspNetCore.Filters;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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

    /// <summary>Last update date of the terms and conditions.</summary>
    public DateTime? LastUpdateDate { get; set; }

    /// <summary>Terms and conditions page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        if (!string.IsNullOrWhiteSpace(UiOptions.TermsUrl) && Uri.IsWellFormedUriString(UiOptions.TermsUrl, UriKind.Absolute)) {
            return await ExternalArticle(UiOptions.TermsUrl, Raw);
        }
        var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var latestTermsReleaseDate = configuration.GetIdentityOption<string?>(nameof(IdentityOptions.SignIn), nameof(ExtendedSignInManager<User>.LatestTermsReleaseDate));
        if (!string.IsNullOrEmpty(latestTermsReleaseDate) && DateTime.TryParseExact(latestTermsReleaseDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var releaseDate)) {
            LastUpdateDate = releaseDate;
            Response.Headers["terms-release-date"] = releaseDate.ToString("o"); // ISO 8601 format
        }
        return await Article("Terms of Service", "~/legal/terms-of-service.md", Raw);
    }
}

internal class TermsModel : BaseTermsModel
{
    public TermsModel() : base() { }
}