using System;
using Indice.Features.Identity.UI.ViewComponents;

namespace Indice.Features.Identity.UI.Models;
/// <summary>
/// View model for the <see cref="PageHeadingViewComponent"/>
/// </summary>
public class PageHeadingViewModel
{
    /// <summary>
    /// View model constructor
    /// </summary>
    /// <param name="title"></param>
    /// <param name="imageSrc"></param>
    public PageHeadingViewModel(string? title, string? imageSrc) {
        Title = title;
        ImageSrc = imageSrc;
    }

    /// <summary>
    /// The logo src/ branding
    /// </summary>
    public string? ImageSrc { get; set; }

    /// <summary>
    /// The page title
    /// </summary>
    public string? Title { get; }
}
