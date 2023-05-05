namespace Indice.Features.Identity.UI.Models;

/// <summary>Represents the view model for an article driven by a markdown file located under the wwwroot folder.</summary>
public class ArticleViewModel
{
    /// <summary>Creates an instance of <see cref="ArticleViewModel"/> class.</summary>
    public ArticleViewModel() : this(string.Empty, string.Empty) { }

    /// <summary>Creates an instance of <see cref="ArticleViewModel"/> class.</summary>
    /// <param name="title">The title of the article.</param>
    /// <param name="markdownPath">The markdown path relative to the wwwroot folder (i.e. <strong>./legal/terms.md</strong>).</param>
    public ArticleViewModel(string title, string markdownPath) {
        Title = title;
        MarkdownPath = markdownPath;
    }

    /// <summary>The title of the article.</summary>
    public string Title { get; set; }
    /// <summary>The markdown path relative to the wwwroot folder.</summary>
    /// <remarks>Example <strong>./legal/terms.md</strong>. Alternatively could be an absolute HTTP URL to a public resource <strong>https://raw.githubusercontent.com/indice-co/Indice.AspNet/master/README.md</strong></remarks>
    public string MarkdownPath { get; set; }
}
