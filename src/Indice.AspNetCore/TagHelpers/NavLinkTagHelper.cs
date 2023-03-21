using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Indice.AspNetCore.TagHelpers;

/// <summary>Anchor link with active trail support (active cssclass)</summary>
public class NavLinkTagHelper : AnchorTagHelper
{
    /// <summary>constructor</summary>
    /// <param name="generator"></param>
    public NavLinkTagHelper(IHtmlGenerator generator)
        : base(generator) {
    }

    /// <summary></summary>
    /// <param name="context"></param>
    /// <param name="output"></param>
    public async override void Process(TagHelperContext context, TagHelperOutput output) {
        base.Process(context, output);
        var childContent = await output.GetChildContentAsync();
        string content = childContent.GetContent();
        output.TagName = "a";
        var hrefAttr = output.Attributes.FirstOrDefault(a => a.Name == "href");
        if (hrefAttr != null) {
            //output.Content.SetHtmlContent($@"<a href=""{hrefAttr.Value}"">{content}</a>");
            //output.Attributes.Remove(hrefAttr);
        } else
            output.Content.SetHtmlContent(content);

        if (ShouldBeActive()) {
            MakeActive(output);
        }
    }

    private bool ShouldBeActive() {
        string currentArea = ViewContext.RouteData.Values["Area"]?.ToString();
        string currentController = ViewContext.RouteData.Values["Controller"]?.ToString();
        string currentAction = ViewContext.RouteData.Values["Action"]?.ToString();
        bool res;
        if (!string.IsNullOrWhiteSpace(Area) && !string.IsNullOrWhiteSpace(Controller) && !string.IsNullOrWhiteSpace(Action))
            res = string.Equals(Area, currentArea, StringComparison.OrdinalIgnoreCase) && string.Equals(Controller, currentController, StringComparison.OrdinalIgnoreCase) && string.Equals(Action, currentAction, StringComparison.OrdinalIgnoreCase);
        else if (!string.IsNullOrWhiteSpace(Controller) && !string.IsNullOrWhiteSpace(Action))
            res = string.Equals(Controller, currentController, StringComparison.OrdinalIgnoreCase) && string.Equals(Action, currentAction, StringComparison.OrdinalIgnoreCase);
        else if (!string.IsNullOrWhiteSpace(Action))
            res = string.Equals(Action, currentAction, StringComparison.OrdinalIgnoreCase);
        else if (!string.IsNullOrWhiteSpace(Controller))
            res = string.Equals(Controller, currentController, StringComparison.OrdinalIgnoreCase);
        else
            res = false;
        return res;
    }

    private void MakeActive(TagHelperOutput output) {
        var classAttr = output.Attributes.FirstOrDefault(a => a.Name == "class");
        if (classAttr == null) {
            classAttr = new TagHelperAttribute("class", "active");
            output.Attributes.Add(classAttr);
        } else if (classAttr.Value == null || classAttr.Value.ToString().IndexOf("active", StringComparison.Ordinal) < 0) {
            output.Attributes.SetAttribute("class", classAttr.Value == null
                ? "active"
                : classAttr.Value + " active");
        }
    }

}
