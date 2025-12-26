using System;
using Indice.Features.Identity.UI.ViewComponents;
using Microsoft.AspNetCore.Html;

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
    public PageHeadingViewModel(IHtmlContent? title, string? imageSrc) {
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
    public IHtmlContent? Title { get; }
}
