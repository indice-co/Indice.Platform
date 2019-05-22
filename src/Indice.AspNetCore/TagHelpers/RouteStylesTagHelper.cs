using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Indice.AspNetCore.TagHelpers
{
    /// <summary>
    /// Tag helper that adds area, controller and action name as CSS classes to html or boady tags.
    /// </summary>
    [HtmlTargetElement("body", Attributes = "[route-styles]")]
    [HtmlTargetElement("html", Attributes = "[route-styles]")]
    public class RouteStylesTagHelper : TagHelper
    {
        /// <summary>
        /// Context for view execution.
        /// </summary>
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        /// <summary>
        /// Synchronously executes the Microsoft.AspNetCore.Razor.TagHelpers.TagHelper with the given context and output.
        /// </summary>
        /// <param name="context">Contains information associated with the current HTML tag.</param>
        /// <param name="output">A stateful HTML element used to generate an HTML tag.</param>
        public override void Process(TagHelperContext context, TagHelperOutput output) {
            base.Process(context, output);
            var area = GetUrlCasing($"{ViewContext.RouteData.Values["area"]}");
            var controller = GetUrlCasing($"{ViewContext.RouteData.Values["controller"]}");
            var action = GetUrlCasing($"{ViewContext.RouteData.Values["action"]}");
            var classList = new List<string>();
            if (output.Attributes.TryGetAttribute("class", out var css)) {
                classList = css.Value.ToString().Split(' ').ToList();
            }
            var pageSpecificClasses = new List<string> { area, controller, action };
            classList.AddRange(pageSpecificClasses.Where(x => !string.IsNullOrEmpty(x)));
            output.Attributes.SetAttribute("class", string.Join(" ", classList));
        }

        private string GetUrlCasing(string pascalCasing) => Regex.Replace(pascalCasing, "([A-Z][a-z]+)", "-$1", RegexOptions.Compiled).Trim().ToLowerInvariant().TrimStart('-');
    }
}
