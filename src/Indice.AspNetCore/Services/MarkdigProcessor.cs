using Indice.Abstractions;
using Markdig;

namespace Indice.Services
{
    public class MarkdigProcessor : IMarkdownProcessor
    {
        public MarkdownPipeline Pipeline { get; } = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        public string Convert(string markdown) => Markdown.ToHtml(markdown, Pipeline);
    }
}
