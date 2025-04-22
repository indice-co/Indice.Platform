using System.Security.Claims;
using Indice.AspNetCore.Filters;
using Indice.Configuration;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the home page screen.</summary>
/// <remarks>Creates a new instance of <see cref="BaseLoginModel"/> class.</remarks>
/// <param name="configuration">Represents a set of key/value application configuration properties.</param>
/// <exception cref="ArgumentNullException"></exception>
[IdentityUI(typeof(HomeModel))]
[SecurityHeaders]
public abstract class BaseHomeModel(IConfiguration configuration) : BasePageModel
{

    /// <summary>Represents a set of key/value application configuration properties.</summary>
    protected IConfiguration Configuration { get; } = configuration;

    /// <summary></summary>
    public List<GatewayServiceModel> Services { get; set; } = [];

    /// <summary>Home page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync() {
        var siteUrl = Configuration[$"{GeneralSettings.Name}:Site"];
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

internal class HomeModel(IConfiguration configuration) : BaseHomeModel(configuration)
{
}