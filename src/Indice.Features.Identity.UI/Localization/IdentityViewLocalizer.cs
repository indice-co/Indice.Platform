using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Localization;

/// <inheritdoc />
public interface IIdentityViewLocalizer : IViewLocalizer { }

internal sealed class IdentityViewLocalizer : IIdentityViewLocalizer, IViewContextAware
{
    private readonly string _applicationName = typeof(IdentityViewLocalizer).Assembly.GetName().Name!;
    private IHtmlLocalizer _internalPathLocalizer = default!;
    private readonly IViewLocalizer _inner;
    private readonly IHtmlLocalizerFactory _localizerFactory;

    public IdentityViewLocalizer(IViewLocalizer inner, IHtmlLocalizerFactory localizerFactory) {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _localizerFactory = localizerFactory ?? throw new ArgumentNullException(nameof(localizerFactory));
    }

    public LocalizedHtmlString this[string name] { 
        get { 
            var resource = _inner[name];
            if (resource.IsResourceNotFound) {
                resource = _internalPathLocalizer[name];
            }
            return resource;
        } 
    }

    public LocalizedHtmlString this[string name, params object[] arguments] {
        get {
            var resource = _inner[name, arguments];
            if (resource.IsResourceNotFound) {
                resource = _internalPathLocalizer[name, arguments];
            }
            return resource;
        }
    }

    public void Contextualize(ViewContext viewContext) {
        ((IViewContextAware)_inner).Contextualize(viewContext);
        // Given a view path "/Views/Home/Index.cshtml" we want a baseName like "MyApplication.Views.Home.Index"
        var path = viewContext.ExecutingFilePath;

        if (string.IsNullOrEmpty(path)) {
            path = viewContext.View.Path;
        }


        Debug.Assert(!string.IsNullOrEmpty(path), "Couldn't determine a path for the view");

        _internalPathLocalizer = _localizerFactory.Create(BuildBaseName(path), _applicationName);
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) {
        var resources = _inner.GetAllStrings(includeParentCultures);
        if (!resources.Any()) {
            resources = _internalPathLocalizer.GetAllStrings(includeParentCultures);
        }
        return resources;
    }

    public LocalizedString GetString(string name) {
        var resource = _inner.GetString(name);
        if (resource.ResourceNotFound) {
            resource = _internalPathLocalizer.GetString(name);
        }
        return resource;
    }

    public LocalizedString GetString(string name, params object[] arguments) {
        var resource = _inner.GetString(name, arguments);
        if (resource.ResourceNotFound) {
            resource = _internalPathLocalizer.GetString(name, arguments);
        }
        return resource;
    }

    private string BuildBaseName(string path) {
        var extension = Path.GetExtension(path);
        var startIndex = path[0] == '/' || path[0] == '\\' ? 1 : 0;
        var length = path.Length - startIndex - extension.Length;
        var capacity = length + _applicationName.Length + 1;
        var builder = new StringBuilder(path, startIndex, length, capacity);

        builder.Replace('/', '.').Replace('\\', '.');

        // Prepend the application name
        builder.Insert(0, '.');
        builder.Insert(0, _applicationName);

        return builder.ToString();
    }
}
