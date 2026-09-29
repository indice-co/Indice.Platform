using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.ViewComponents;
/// <summary>
/// Renders the hero panel (title and subtitle) next to the page content. Render it through the <c>hero</c> layout section.
/// </summary>
[ViewComponent(Name = "HeroPanel")]
public class HeroPanelViewComponent : ViewComponent
{
    private readonly IdentityUILocalizer _localizer;

    /// <summary>
    /// Constructs the HeroPanel view component
    /// </summary>
    /// <param name="localizer">The Identity UI localizer.</param>
    public HeroPanelViewComponent(IdentityUILocalizer localizer) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>Renders the hero panel.</summary>
    /// <param name="model">Optional hero texts. Any value left null falls back to the default localized text.</param>
    public IViewComponentResult Invoke(HeroViewModel? model = null) {
        return View(new HeroViewModel(model?.Title ?? _localizer.Hero_Title, model?.Subtitle ?? _localizer.Hero_Subtitle));
    }
}
