using System.IO;
using Markdig;

namespace Indice.Services
{
    /// <summary>
    /// Implementation for <see cref="IMarkdownProcessor"/> using markdig lib.
    /// </summary>
    public class MarkdigProcessor : IMarkdownProcessor
    {
        /// <summary>
        /// pipeline to use
        /// </summary>
        public MarkdownPipeline Pipeline { get; } = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        /// <summary>
        /// Converts to html
        /// </summary>
        /// <param name="markdown"></param>
        /// <returns></returns>
        public string Convert(string markdown) => Markdown.ToHtml(markdown, Pipeline);

        /// <summary>
        /// Converts to html
        /// </summary>
        /// <param name="markdown"></param>
        /// <param name="writer"></param>
        public void Convert(string markdown, TextWriter writer) => Markdown.ToHtml(markdown, writer, Pipeline);
    }
}
