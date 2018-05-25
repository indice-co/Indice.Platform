using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Indice.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.TagHelpers
{
    public class MdTagHelper : TagHelper
    {
        private IHostingEnvironment env;
        private ILogger<MdTagHelper> logger;
        private IMarkdownProcessor markdownProcessor;

        public MdTagHelper(IHostingEnvironment env, ILogger<MdTagHelper> logger, IMarkdownProcessor markdownProcessor) {
            this.env = env ?? throw new ArgumentNullException(paramName: nameof(env));
            this.logger = logger ?? throw new ArgumentNullException(paramName: nameof(logger));
            this.markdownProcessor = markdownProcessor ?? throw new ArgumentNullException(paramName: nameof(markdownProcessor));
        }

        [HtmlAttributeName(name: "path")]
        public string Path { get; set; }

        [HtmlAttributeName(name: "href")]
        public string HRef { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output) {
            var md = string.Empty;

            if (Path != null) {
                try {
                    if (Path.StartsWith("~")) {
                        Path = Path.Replace("~", env.WebRootPath);
                    }

                    using (var reader = File.OpenText(Path)) {
                        md = await reader.ReadToEndAsync();
                    }
                } catch (Exception ex) {
                    md = $"Problem reading file at {Path}";
                    logger.LogError(eventId: 0, exception: ex, message: md);
                }
            } else if (HRef != null) {
                try {
                    using (var httpClient = new HttpClient()) {
                        md = await httpClient.GetStringAsync(HRef);
                    }
                } catch (Exception ex) {
                    md = $"Problem reading url {HRef}";
                    logger.LogError(eventId: 0, exception: ex, message: md);
                }
            } else if (Path == null && HRef == null) {
                var result = await output.GetChildContentAsync();
                // Get markdown from inner text.
                md = result.GetContent();
            }

            output.TagName = "section";

            if (!output.Attributes.ContainsName("class")) {
                output.Attributes.Add("class", "markdown");
            }

            if (md != string.Empty) {
                var mdAsHtml = markdownProcessor.Convert(md);
                output.Content.AppendHtml(mdAsHtml);
            }
        }
    }
}
