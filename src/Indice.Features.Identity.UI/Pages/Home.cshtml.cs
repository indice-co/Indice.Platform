using Indice.AspNetCore.Filters;
using Indice.Configuration;
using Indice.Features.Identity.UI.Models;
using Indice.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the home/landing screen.</summary>
[SecurityHeaders]
public class HomePageModel : PageModel
{
    private readonly ILogger<HomePageModel> _logger;
    private readonly IStringLocalizer<HomePageModel> _localizer;
    private readonly IConfiguration _configuration;

    /// <summary>Creates a new instance of <see cref="LoginPageModel"/> class.</summary>
    /// <param name="logger">A generic interface for logging.</param>
    /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="LoginPageModel"/>.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public HomePageModel(
        ILogger<HomePageModel> logger,
        IStringLocalizer<HomePageModel> localizer,
        IConfiguration configuration
    ) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _configuration = configuration;
    }

    /// <summary></summary>
    public List<GatewayServiceModel> Services { get; set; }

    /// <summary>Home page GET handler.</summary>
    public IActionResult OnGet() {
        var siteUrl = _configuration[$"{GeneralSettings.Name}:Site"];
        if (!string.IsNullOrWhiteSpace(siteUrl)) {
            return Redirect(siteUrl);
        }
        Services = new List<GatewayServiceModel> {
            new GatewayServiceModel {
                DisplayName = "Admin",
                ImageSrc = null,
                Link = "~/admin",
                Visible = User.IsAdmin()
            }
        };
        return Page();
    }
}
