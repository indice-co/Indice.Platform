using System.Data;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

internal partial class IdentityUIPageRouteModelConvention2 : IPageRouteModelConvention
{
    private readonly string _uiFramework;
    private readonly Regex _frameworkPattern = FrameworkPattern();
    public IdentityUIPageRouteModelConvention2(string uiFramework) {
        _uiFramework = uiFramework;
    }
    public void Apply(PageRouteModel model) {
        // Only modify pages in the UiFrameworks folder.
        if (!model.ViewEnginePath.StartsWith("/UiFrameworks/"))
            return;

        // UiFrameworks/<bootstrap>/<page>...
        if (!TryParseFrameworkFromPath(model.ViewEnginePath, out var uiFramework))
            return;
        foreach (var selector in model.Selectors) {
            var constraint = new IdentityUiFrameworkActionConstraint(uiFramework);
            selector.AttributeRouteModel!.Template = StripOffTheFramewokPath(selector.AttributeRouteModel.Template);

            // Add the constraint which will restrict the page from being
            // chosen unless the request's theme matches the page theme
            selector.ActionConstraints.Add(constraint);

        }
    }

    private string? StripOffTheFramewokPath(string? template) {
        if (template == null) return null;
        return template.Replace($"{_uiFramework}/", string.Empty);
    }

    private bool TryParseFrameworkFromPath(string viewEnginePath, out string uiFramework) {
        uiFramework = string.Empty;
        var mathch = _frameworkPattern.Match(viewEnginePath);
        if (mathch.Success) {
            uiFramework = mathch.Groups[1].Value;
            return true;
        }
        return false;
    }
#if NET7_0_OR_GREATER
    [GeneratedRegex("/UiFrameworks/([A-Za-z0-9_]+)/(([A-Za-z0-9_]+))")]
    private static partial Regex FrameworkPattern();
#else
    private static Regex FrameworkPattern() => new Regex("/UiFrameworks/([A-Za-z0-9_]+)/(([A-Za-z0-9_]+))");
#endif
}

internal class IdentityUiFrameworkActionConstraint : IActionConstraint
{
    public int Order => -1;

    private string _uiFramework;

    public IdentityUiFrameworkActionConstraint(string uiFramework) {
        _uiFramework = uiFramework;
    }

    public bool Accept(ActionConstraintContext context) {

        var options = context.RouteContext.HttpContext.RequestServices.GetRequiredService<IOptions<IdentityUIOptions>>();

        return _uiFramework.Equals(options.Value.UiFramework);
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
            //var options = context.RouteContext.HttpContext.RequestServices.GetRequiredService<IOptions<IdentityUIOptions>>();
        }
        return true;// _uiFramework.Equals(options.Value.UiFramework);
    }
}
