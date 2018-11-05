using System.IO;

namespace Indice.Services
{
    /// <summary>
    /// A processing service that converts input markdown string to html string
    /// </summary>
    public interface IMarkdownProcessor
    {
        /// <summary>
        /// Converts the source markdown to the destination html string.
        /// </summary>
        /// <param name="markdown"></param>
        /// <returns></returns>
        string Convert(string markdown);

        /// <summary>
        /// Converts the source markdown to the destination html and writes the output to the <paramref name="writer"/>.
        /// </summary>
        /// <param name="markdown"></param>
        /// <param name="writer">The destination <see cref="TextWriter"/> that will receive the result of the conversion.</param>
        /// <returns></returns>
        void Convert(string markdown, TextWriter writer);
    }
}
