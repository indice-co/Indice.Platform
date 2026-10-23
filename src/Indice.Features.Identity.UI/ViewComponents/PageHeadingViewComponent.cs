using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.ViewComponents;
/// <summary>
/// Used to show the page title, an optional subtitle and an optional image.
/// </summary>
[ViewComponent(Name = "PageHeading")]
public class PageHeadingViewComponent : ViewComponent
{
    /// <summary>
    /// Constructs the PageHeading view component
    /// </summary>
    public PageHeadingViewComponent() {

    }

    /// <summary>Renders the heading.</summary>
    /// <param name="title">The page title.</param>
    /// <param name="imageSrc">An optional image rendered above the title. Leave empty to render no image.</param>
    /// <param name="subtitle">An optional subtitle rendered under the title.</param>
    public IViewComponentResult Invoke(IHtmlContent? title, string? imageSrc, IHtmlContent? subtitle = null) {
        return View(new PageHeadingViewModel(title, string.IsNullOrEmpty(imageSrc) ? null : Url.Content(imageSrc), subtitle));
    }

}
