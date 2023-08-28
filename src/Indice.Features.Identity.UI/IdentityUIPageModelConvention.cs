using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Indice.Features.Identity.UI;

internal class IdentityUIPageModelConvention : IPageApplicationModelConvention
{
    public void Apply(PageApplicationModel model) {
        var identityUIAttribute = model.ModelType?.GetCustomAttribute<IdentityUIAttribute>();
        if (identityUIAttribute is null) {
            return;
        }
        model.ModelType = identityUIAttribute.Template.GetTypeInfo();
    }
}

internal partial class IdentityUIPageRouteModelConvention : IPageRouteModelConvention
{
    public IdentityUIPageRouteModelConvention() {
    }
    public void Apply(PageRouteModel model) {
        // Only modify pages in the Themes folder.
        if (!model.ViewEnginePath.StartsWith("/Themes/"))
            return;

        foreach (var selector in model.Selectors) {
            selector.ActionConstraints.Add(new IdentityUiThemeActionConstraint());
        }
    }
}

internal class IdentityUiThemeActionConstraint : IActionConstraint
{
    public int Order => -1;


    public IdentityUiThemeActionConstraint() {

    }

    public bool Accept(ActionConstraintContext context) {
        if (context.CurrentCandidate.Action is Microsoft.AspNetCore.Mvc.RazorPages.CompiledPageActionDescriptor pageAction) {
            var themeAttribute = pageAction.ModelTypeInfo?.GetCustomAttribute<ClientThemeAttribute>();
            if (themeAttribute is null) {
                return true;
            }
            var requestClientId = context.RouteContext.HttpContext.GetClientIdFromReturnUrl();
            var pageClientIds = themeAttribute.ClientIds;
            var reject = string.IsNullOrWhiteSpace(requestClientId) || !pageClientIds.Contains(requestClientId, StringComparer.OrdinalIgnoreCase);
            return !reject;
        }
        return true;
    }
}
