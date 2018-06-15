using Markdig;

namespace Indice.Services
{
    /// <summary>
    /// Implementation for <see cref="IMarkdownProcessor"/> using markdig lib.
    /// </summary>
    public class MarkdigProcessor : IMarkdownProcessor
    {
        public MarkdownPipeline Pipeline { get; } = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        public string Convert(string markdown) => Markdown.ToHtml(markdown, Pipeline);
    }
}
