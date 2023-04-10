using System;

namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Describes a blog post item.</summary>
public class BlogItemInfo
{
    /// <summary>Title of the post.</summary>
    public string? Title { get; set; }
    /// <summary>Original link to the post.</summary>
    public string? Link { get; set; }
    /// <summary>The datetime that the post was published.</summary>
    public DateTime? PublishDate { get; set; }
    /// <summary>A small description for the post.</summary>
    public string? Description { get; set; }
}
