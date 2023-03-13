using Markdig;

namespace Indice.Services;

/// <summary>Implementation for <see cref="IMarkdownProcessor"/> using markdig lib.</summary>
public class MarkdigProcessor : IMarkdownProcessor
{
    /// <summary>Pipeline to use.</summary>
    public MarkdownPipeline Pipeline { get; } = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    
    /// <summary>Converts to HTML.</summary>
    /// <param name="markdown">The markdown text.</param>
    public string Convert(string markdown) => Markdown.ToHtml(markdown, Pipeline);

    /// <summary>Converts to HTML.</summary>
    /// <param name="markdown">The markdown text.</param>
    /// <param name="writer">The destination <see cref="TextWriter"/> that will receive the result of the conversion.</param>
    public void Convert(string markdown, TextWriter writer) => Markdown.ToHtml(markdown, writer, Pipeline);
}
