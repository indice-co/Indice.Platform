using System.Globalization;
using System.Net.Mime;
using System.Text;
using HtmlAgilityPack;
using Indice.Features.Identity.UI.Models;
using Indice.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Base model class for article pages.</summary>
public abstract class BaseArticlePageModel : PageModel
{
    /// <summary>Defines a mechanism for retrieving a service object.</summary>
    protected IServiceProvider ServiceProvider => HttpContext.RequestServices;

    private IdentityUIOptions? _UiOptions;
    /// <summary>UI Options</summary>
    public IdentityUIOptions UiOptions => _UiOptions ??= ServiceProvider.GetRequiredService<IOptions<IdentityUIOptions>>().Value;

    /// <summary>The article view model.</summary>
    public ArticleViewModel View { get; set; } = new ArticleViewModel();

    /// <summary>Will propagate to body class.</summary>
    [ViewData]
    public virtual string BodyCssClass { get; set; } = "identity-page article-page";

    /// <summary>Renders the given article using the provided <paramref name="title"/> and <paramref name="markdownPath"/>.</summary>
    /// <param name="title"></param>
    /// <param name="markdownPath"></param>
    /// <param name="raw">Will render only raw markdown converted to HTML.</param>
    protected async Task<IActionResult> Article(string title, string markdownPath, bool? raw = null) {
        var localizerType = typeof(IStringLocalizer<>).MakeGenericType(GetType());
        var localizer = (IStringLocalizer)ServiceProvider.GetRequiredService(localizerType);
        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var fileName = Path.GetFileNameWithoutExtension(markdownPath);
        var localizedFileName = $"{fileName}.{lang}";
        var environment = ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var fileProvider = ServiceProvider.GetService<IOptions<StaticFileOptions>>()?.Value?.FileProvider ?? environment.WebRootFileProvider;
        var file = fileProvider.GetFileInfo(markdownPath.TrimStart('~', '/').Replace(fileName, localizedFileName));
        if (file.Exists) {
            markdownPath = markdownPath.Replace(fileName, localizedFileName);
        } else {
            file = fileProvider.GetFileInfo(markdownPath.TrimStart('~', '/'));
        }
        if (raw.HasValue && raw == true) {
            var markdownProcessor = ServiceProvider.GetRequiredService<IMarkdownProcessor>();
            if (!file.Exists) {
                return RedirectToPage("/Error40X", new { statusCode = 404 });
            }
            var markdownText = string.Empty;
            using (var streamReader = new StreamReader(file.CreateReadStream())) {
                markdownText = await streamReader.ReadToEndAsync();
            }
            return Content(markdownText is not null ? markdownProcessor.Convert(markdownText) : string.Empty, "text/html; charset=utf-8");
        }
        View = new ArticleViewModel(localizer[title], markdownPath);
        return Page();
    }

    /// <summary>
    /// Redirect to an external article or Loads the rich text from the external html source
    /// </summary>
    /// <param name="articleUrl">The external source</param>
    /// <param name="raw">Load the raw html.</param>
    /// <returns></returns>
    protected async Task<IActionResult> ExternalArticle(string articleUrl, bool? raw = null) {
        if (raw.HasValue && raw.Value) {
            var http = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient();
            var html = await http.GetStringAsync(articleUrl);
            var doc = new HtmlDocument();
            doc.OptionFixNestedTags = true;
            doc.OptionAutoCloseOnEnd = true;
            doc.OptionDefaultStreamEncoding = Encoding.UTF8;
            doc.OptionWriteEmptyNodes = true;
            doc.OptionOutputAsXml = true;
            doc.LoadHtml(html);
            var mainNode = doc.DocumentNode.SelectSingleNode("//main")!;
            var articleNode = mainNode.SelectSingleNode("//article");
            if (articleNode is not null) {
                mainNode = articleNode;
            }
            CleanNodes(mainNode, ["p", "br", "strong", "em", "b", "ul", "li", "ol", "a", "table", "tbody", "thead", "tfoot", "tr", "td", "h1", "h2", "h3", "h4", "h5", "h6", "h7", "h8"], ["href"]);
            return Content(HtmlEntity.DeEntitize(mainNode.InnerHtml)!, MediaTypeNames.Text.Html, Encoding.UTF8);
        }
        return Redirect(articleUrl);
    }

    /// <summary>
    /// Recursively delete nodes not in the attributeWhitelist
    /// </summary>
    private HtmlNode CleanNodes(HtmlNode node, string[] nodeWhitelist, string[] attributeWhitelist) {
        if (SkipNode(node)) {
            var nextNode = node.NextSibling;
            node.ParentNode.RemoveChild(node);

            return nextNode;
        }

        if (node.HasChildNodes) {
            var childNode = node.FirstChild;
            while (childNode != null) {
                childNode = CleanNodes(childNode, nodeWhitelist, attributeWhitelist);
            }
        }

        if (node.NodeType == HtmlNodeType.Element) {
            var attribs = node.Attributes.ToList();
            for (int i = attribs.Count - 1; i >= 0; i--) {
                var attrib = node.Attributes[i];
                if (!attributeWhitelist.Contains(attrib.Name)) {
                    node.Attributes.Remove(attrib);
                }
            }

            if (!nodeWhitelist.Contains(node.Name)) {
                var nodeList = node.ChildNodes.ToList();
                foreach (var child in nodeList) {
                    node.ParentNode.InsertBefore(child, node);
                }

                var nextNode = node.NextSibling;
                node.ParentNode.RemoveChild(node);

                return nextNode;
            }
        }

        return node.NextSibling;
    }

    private bool SkipNode(HtmlNode node) {
        if (node.NodeType == HtmlNodeType.Comment) {
            return true;
        }
        if (node.Name == "script" || node.Name == "style" || node.Name == "aside") {
            return true;
        }
        if (node.NodeType == HtmlNodeType.Text && String.IsNullOrWhiteSpace(node.InnerText)) {
            return true;
        }
        return false;
    }
}
