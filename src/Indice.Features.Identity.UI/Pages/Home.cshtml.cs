using System.Security.Claims;
using Indice.AspNetCore.Filters;
using Indice.Configuration;
using Indice.Features.Identity.UI.Localization;
using Indice.Features.Identity.UI.Models;
using Indice.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the home page screen.</summary>
[IdentityUI(typeof(HomeModel))]
[SecurityHeaders]
public abstract class BaseHomeModel : BasePageModel
{
    private readonly IConfiguration _configuration;

    /// <summary>Creates a new instance of <see cref="BaseLoginModel"/> class.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseHomeModel(IConfiguration configuration) {
        _configuration = configuration;
    }

    /// <summary></summary>
    public List<GatewayServiceModel> Services { get; set; } = new List<GatewayServiceModel>();

    /// <summary>Home page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        var siteUrl = _configuration[$"{GeneralSettings.Name}:Site"];
        if (!string.IsNullOrWhiteSpace(siteUrl)) {
            return await Task.FromResult(Redirect(siteUrl));
        }
        Services.AddRange(UiOptions.HomepageLinks.Select(x => new GatewayServiceModel {
            DisplayName = x.DisplayName,
            CssClass = x.CssClass,
            ImageSrc = x.ImageSrc,
            Link = x.Link,
            Visible = (x.VisibilityPredicate ?? new Predicate<ClaimsPrincipal>(principal => true))(User)
        }));
        return Page();
    }
}

internal class HomeModel : BaseHomeModel
{
    public HomeModel(IConfiguration configuration) : base(configuration) { }
}