using System.Text;
using Indice.AspNetCore.EmbeddedUI;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Helper extensions that manipulate the <see cref="SpaUIOptions"/>.
    /// </summary>
    public static class SpaUIOptionsExtensions
    {
        /// <summary>
        /// Injects additional JavaScript files into the index.html page.
        /// </summary>
        /// <param name="options">Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</param>
        /// <param name="path">A path to the JavaScript - i.e. the script "src" attribute.</param>
        /// <param name="type">The script type - i.e. the script "type" attribute.</param>
        /// <returns></returns>
        public static SpaUIOptions InjectJavascript(this SpaUIOptions options, string path, string type = "text/javascript") {
            var builder = new StringBuilder(options.HeadContent);
            builder.AppendLine($"<script src='{path}' type='{type}'></script>");
            options.HeadContent = builder.ToString();
            return options;
        }

        /// <summary>
        /// Injects additional CSS style-sheets into the index.html page.
        /// </summary>
        /// <param name="options">Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</param>
        /// <param name="path">A path to the style-sheet - i.e. the link "href" attribute.</param>
        /// <param name="media">The target media - i.e. the link "media" attribute.</param>
        public static SpaUIOptions InjectStylesheet(this SpaUIOptions options, string path, string media = "screen") {
            var builder = new StringBuilder(options.HeadContent);
            builder.AppendLine($"<link href='{path}' rel='stylesheet' media='{media}' type='text/css' />");
            options.HeadContent = builder.ToString();
            return options;
        }
    }
}
