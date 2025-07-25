using Indice.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.TagHelpers;

/// <summary>Markdown tag helper.</summary>
public class MdTagHelper : TagHelper
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MdTagHelper> _logger;
    private readonly IMarkdownProcessor _markdownProcessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFileProvider _fileProvider;

    /// <summary>constructs the helper</summary>
    /// <param name="environment">The hosting environment</param>
    /// <param name="logger">The logger</param>
    /// <param name="markdownProcessor"></param>
    /// <param name="httpClientFactory"></param>
    /// <param name="staticFileOptions"></param>
    public MdTagHelper(IWebHostEnvironment environment, ILogger<MdTagHelper> logger, IMarkdownProcessor markdownProcessor, IHttpClientFactory httpClientFactory, IOptions<StaticFileOptions> staticFileOptions) {
        _env = environment ?? throw new ArgumentNullException(paramName: nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(paramName: nameof(logger));
        _markdownProcessor = markdownProcessor ?? throw new ArgumentNullException(paramName: nameof(markdownProcessor));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _fileProvider = staticFileOptions.Value.FileProvider!;
    }

    /// <summary>local server path</summary>
    [HtmlAttributeName(name: "path")]
    public string? Path { get; set; }

    /// <summary>Used to download markdown from the web</summary>
    [HtmlAttributeName(name: "href")]
    public string? HRef { get; set; }

    /// <inheritdoc/>
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output) {
        var md = string.Empty;

        if (Path != null) {
            try {
                
                var file = (_fileProvider ?? _env.WebRootFileProvider).GetFileInfo(Path.TrimStart('~', '/'));
                using var reader = new StreamReader(file.CreateReadStream());
                md = await reader.ReadToEndAsync();
            } catch (Exception ex) {
                md = $"Problem reading file at {Path}";
                _logger.LogError(eventId: 0, exception: ex, message: md);
            }
        } else if (HRef != null) {
            try {
                var httpClient = _httpClientFactory.CreateClient();
                md = await httpClient.GetStringAsync(HRef);
            } catch (Exception ex) {
                md = $"Problem reading url {HRef}";
                _logger.LogError(eventId: 0, exception: ex, message: md);
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
            var mdAsHtml = _markdownProcessor.Convert(md);
            output.Content.AppendHtml(mdAsHtml);
        }
    }
}
