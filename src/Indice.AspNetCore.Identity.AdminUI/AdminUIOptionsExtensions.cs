using System.Text;
using Indice.AspNetCore.Identity.AdminUI;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Helper extensions that manipulate the <see cref="AdminUIOptions"/>.
    /// </summary>
    public static class AdminUIOptionsExtensions
    {
        /// <summary>
        /// Injects additional Javascript files into the index.html page.
        /// </summary>
        /// <param name="options">Options for configuring <see cref="AdminUIMiddleware"/> middleware.</param>
        /// <param name="path">A path to the javascript - i.e. the script "src" attribute.</param>
        /// <param name="type">The script type - i.e. the script "type" attribute.</param>
        /// <returns></returns>
        public static AdminUIOptions InjectJavascript(this AdminUIOptions options, string path, string type = "text/javascript") {
            var builder = new StringBuilder(options.HeadContent);
            builder.AppendLine($"<script src='{path}' type='{type}'></script>");
            options.HeadContent = builder.ToString();
            return options;
        }

        /// <summary>
        /// Injects additional CSS stylesheets into the index.html page.
        /// </summary>
        /// <param name="options">Options for configuring <see cref="AdminUIMiddleware"/> middleware.</param>
        /// <param name="path">A path to the stylesheet - i.e. the link "href" attribute.</param>
        /// <param name="media">The target media - i.e. the link "media" attribute.</param>
        public static AdminUIOptions InjectStylesheet(this AdminUIOptions options, string path, string media = "screen") {
            var builder = new StringBuilder(options.HeadContent);
            builder.AppendLine($"<link href='{path}' rel='stylesheet' media='{media}' type='text/css' />");
            options.HeadContent = builder.ToString();
            return options;
        }
    }
}
