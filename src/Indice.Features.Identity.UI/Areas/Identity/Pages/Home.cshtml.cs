using Indice.AspNetCore.Filters;
using Indice.Configuration;
using Indice.Features.Identity.UI.Areas.Identity.Models;
using Indice.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Identity.UI.Areas.Identity.Pages;

/// <summary>Page model for the home/landing screen.</summary>
[SecurityHeaders]
public class HomeModel : PageModel
{
    private readonly ILogger<HomeModel> _logger;
    private readonly IStringLocalizer<HomeModel> _localizer;
    private readonly IConfiguration _configuration;

    /// <summary>Creates a new instance of <see cref="LoginModel"/> class.</summary>
    /// <param name="logger">A generic interface for logging.</param>
    /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="LoginModel"/>.</param>
    /// <param name="configuration"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public HomeModel(
        ILogger<HomeModel> logger,
        IStringLocalizer<HomeModel> localizer,
        IConfiguration configuration
    ) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _configuration = configuration;
    }

    /// <summary></summary>
    public List<HomeServiceModel> Services { get; set; }

    /// <summary>Home page GET handler.</summary>
    public IActionResult OnGet() {
        var siteUrl = _configuration[$"{GeneralSettings.Name}:Site"];
        if (!string.IsNullOrWhiteSpace(siteUrl)) {
            return Redirect(siteUrl);
        }
        Services = new List<HomeServiceModel> {
            new HomeServiceModel {
                DisplayName = "Admin",
                ImageSrc = null,
                Link = "~/admin",
                Visible = User.IsAdmin()
            }
        };
        return Page();
    }
}
