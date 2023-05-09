using Indice.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Razor;

namespace Indice.Features.Identity.UI;

internal class ClientThemesViewLocationExpander : IViewLocationExpander
{
    private const string ValueKey = "client_id";
    private const string ClientThemesFolderName = "Themes";

    public void PopulateValues(ViewLocationExpanderContext context) {
        var clientId = context.ActionContext.HttpContext.GetClientIdFromReturnUrl();
        context.Values[ValueKey] = clientId ?? string.Empty;
    }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations) {
        context.Values.TryGetValue(ValueKey, out var value);
        if (string.IsNullOrEmpty(value)) {
            return viewLocations.Where(x => !x.StartsWith($"/Pages/{ClientThemesFolderName}", StringComparison.OrdinalIgnoreCase));
        }
        return viewLocations;
    }
}
