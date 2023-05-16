using System.Globalization;
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
            file = fileProvider.GetFileInfo(markdownPath.TrimStart('~', '/').Replace(fileName, localizedFileName));
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
}
