using Indice.Features.Identity.UI.ViewComponents;
using Microsoft.AspNetCore.Html;

namespace Indice.Features.Identity.UI.Models;
/// <summary>
/// View model for the <see cref="HeroPanelViewComponent"/>
/// </summary>
public class HeroViewModel
{
    /// <summary>
    /// View model constructor
    /// </summary>
    /// <param name="title">The hero title. When null the default localized title is used.</param>
    /// <param name="subtitle">The hero subtitle. When null the default localized subtitle is used.</param>
    public HeroViewModel(IHtmlContent? title = null, IHtmlContent? subtitle = null) {
        Title = title;
        Subtitle = subtitle;
    }

    /// <summary>
    /// The hero title.
    /// </summary>
    public IHtmlContent? Title { get; }

    /// <summary>
    /// The hero subtitle rendered under the title.
    /// </summary>
    public IHtmlContent? Subtitle { get; }
}
