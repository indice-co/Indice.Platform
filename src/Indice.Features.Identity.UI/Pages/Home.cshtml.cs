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

/// <summary>Page model for the home page screen.</summary>
[IdentityUI(typeof(HomeModel))]
[SecurityHeaders]
public abstract class BaseHomeModel : PageModel
{
    private readonly ILogger<BaseHomeModel> _logger;
    private readonly IStringLocalizer<BaseHomeModel> _localizer;
    private readonly IConfiguration _configuration;

    /// <summary>Creates a new instance of <see cref="BaseLoginModel"/> class.</summary>
    /// <param name="logger">A generic interface for logging.</param>
    /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="BaseLoginModel"/>.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseHomeModel(
        ILogger<BaseHomeModel> logger,
        IStringLocalizer<BaseHomeModel> localizer,
        IConfiguration configuration
    ) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
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
        Services.AddRange(new List<GatewayServiceModel> {
            new GatewayServiceModel {
                DisplayName = "Admin",
                ImageSrc = null,
                Link = "~/admin",
                Visible = User.IsAdmin()
            }
        });
        return Page();
    }
}

internal class HomeModel : BaseHomeModel
{
    public HomeModel(
        ILogger<BaseHomeModel> logger,
        IStringLocalizer<BaseHomeModel> localizer,
        IConfiguration configuration
    ) : base(logger, localizer, configuration) { }
}