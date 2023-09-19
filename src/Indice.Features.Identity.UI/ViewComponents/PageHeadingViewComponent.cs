using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.ViewComponents;
/// <summary>
/// Used to show the page title as well as the logo branding.
/// </summary>
[ViewComponent(Name = "PageHeading")]
public class PageHeadingViewComponent : ViewComponent
{
    /// <summary>
    /// Constructs the PageHeading view component
    /// </summary>
    public PageHeadingViewComponent() {
            
    }


    /// <inheritdoc/>
    public IViewComponentResult Invoke(string? title, string? imageSrc) {
        return View(new PageHeadingViewModel(title, string.IsNullOrEmpty(imageSrc) ? null : Url.Content(imageSrc)));
    }

}
