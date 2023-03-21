using Indice.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.TagHelpers;

/// <summary>Markdown tag helper.</summary>
public class MdTagHelper : TagHelper
{
    private IWebHostEnvironment env;
    private ILogger<MdTagHelper> logger;
    private IMarkdownProcessor markdownProcessor;

    /// <summary>constructs the helper</summary>
    /// <param name="env"></param>
    /// <param name="logger"></param>
    /// <param name="markdownProcessor"></param>
    public MdTagHelper(IWebHostEnvironment env, ILogger<MdTagHelper> logger, IMarkdownProcessor markdownProcessor) {
        this.env = env ?? throw new ArgumentNullException(paramName: nameof(env));
        this.logger = logger ?? throw new ArgumentNullException(paramName: nameof(logger));
        this.markdownProcessor = markdownProcessor ?? throw new ArgumentNullException(paramName: nameof(markdownProcessor));
    }

    /// <summary>local server path</summary>
    [HtmlAttributeName(name: "path")]
    public string Path { get; set; }

    /// <summary>Used to download markdown from the web</summary>
    [HtmlAttributeName(name: "href")]
    public string HRef { get; set; }

    /// <summary>Process</summary>
    /// <param name="context"></param>
    /// <param name="output"></param>
    /// <returns></returns>
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
