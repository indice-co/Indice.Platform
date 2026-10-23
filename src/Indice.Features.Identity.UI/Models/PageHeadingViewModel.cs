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
    /// <param name="title">The page title.</param>
    /// <param name="imageSrc">An optional image rendered above the title.</param>
    /// <param name="subtitle">An optional subtitle rendered under the title.</param>
    public PageHeadingViewModel(IHtmlContent? title, string? imageSrc, IHtmlContent? subtitle = null) {
        Title = title;
        ImageSrc = imageSrc;
        Subtitle = subtitle;
    }

    /// <summary>
    /// An optional image (for example a client logo) rendered above the title. When empty nothing is rendered.
    /// </summary>
    public string? ImageSrc { get; set; }

    /// <summary>
    /// The page title
    /// </summary>
    public IHtmlContent? Title { get; }

    /// <summary>
    /// An optional subtitle rendered under the title.
    /// </summary>
    public IHtmlContent? Subtitle { get; }
}
