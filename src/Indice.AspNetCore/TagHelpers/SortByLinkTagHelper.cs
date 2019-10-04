using System.Linq;
using System.Threading.Tasks;
using Indice.Types;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Indice.AspNetCore.TagHelpers
{
    /// <summary>
    /// Tag helper for data grids. This one creates the correct sort by link and css classes 
    /// given the sort by property path expression and the current route data. 
    /// </summary>
    [HtmlTargetElement("a", Attributes = "sort-by")]
    public class SortByLinkTagHelper : AnchorTagHelper
    {
        /// <summary>
        /// Tag helper constructor.
        /// </summary>
        /// <param name="generator"></param>
        public SortByLinkTagHelper(IHtmlGenerator generator) : base(generator) { }

        /// <summary>
        /// the sort by property path expression for the column we need to sort.
        /// </summary>
        public ModelExpression SortBy { get; set; }

        /// <summary>
        /// The list options passed on the current route
        /// </summary>
        public ListOptions ListOptions { get; set; }

        /// <summary>
        /// tag helper initialization.
        /// </summary>
        /// <param name="context"></param>
        public override void Init(TagHelperContext context) {
            RouteValues = ListOptions.ToDictionary(new { sort = SortBy.Metadata.PropertyName }).AsRouteValues().ToDictionary(x => x.Key, x => x.Value); // this last thing is a problem
            base.Init(context);
        }

        /// <summary>
        /// Do work
        /// </summary>
        /// <param name="context"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public async override Task ProcessAsync(TagHelperContext context, TagHelperOutput output) {
            await base.ProcessAsync(context, output);
            var childContent = await output.GetChildContentAsync();
            if (childContent.IsEmptyOrWhiteSpace) {
                var spanTag = new TagBuilder("span");
                spanTag.InnerHtml.Append(SortBy.Metadata.GetDisplayName());
                output.Content.SetHtmlContent(spanTag);
            }
        }
    }
}
