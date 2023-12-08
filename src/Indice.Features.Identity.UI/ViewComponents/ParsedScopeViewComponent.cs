using Indice.Features.Identity.Core.Scopes;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Identity.UI.ViewComponents;

/// <summary>
/// Profile parsed scope View component.
/// </summary>
public class ParsedScopeViewComponent : ViewComponent
{
    /// <summary>
    /// Constructs the parsed scope view component
    /// </summary>
    public ParsedScopeViewComponent() {
    }

    /// <inheritdoc/>
    public IViewComponentResult Invoke(ParsedScopeMetadata model) {
        return View(model.GetType().Name, model);
    }
}