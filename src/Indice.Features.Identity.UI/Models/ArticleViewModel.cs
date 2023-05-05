namespace Indice.Features.Identity.UI.Models;

/// <summary>Represents the view model for an article driven by a markdown file located under the wwwroot.</summary>
public class ArticleViewModel
{
    /// <summary>Creates a default article view model</summary>
    public ArticleViewModel() : this(string.Empty, string.Empty){
        
    }

    /// <summary>Creates the article view model</summary>
    /// <param name="title">The title</param>
    /// <param name="markdownPath">The markdown path <strong>./legal/terms.md</strong></param>
    public ArticleViewModel(string title, string markdownPath) {
        Title = title;
        MarkdownPath = markdownPath;
    }
    /// <summary>The article title</summary>
    public string Title { get; set; }
    /// <summary>The markdown path. This is relative to the wwwroot folder</summary>
    /// <remarks>
    /// Example <strong>./legal/terms.md</strong>. 
    /// Alternatively could be an aboslute http uri to a public resource <strong>https://raw.githubusercontent.com/indice-co/Indice.AspNet/master/README.md</strong>
    /// </remarks>
    public string MarkdownPath { get; set; }
}
