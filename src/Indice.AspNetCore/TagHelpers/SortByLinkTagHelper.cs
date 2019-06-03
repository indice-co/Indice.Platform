using System.Linq;
using System.Threading.Tasks;
using Indice.Types;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Indice.AspNetCore.TagHelpers
{
    [HtmlTargetElement("a", Attributes = "sort-by")]
    public class SortByLinkTagHelper : AnchorTagHelper
    {
        public SortByLinkTagHelper(IHtmlGenerator generator) : base(generator) { }

        public ModelExpression SortBy { get; set; }
        public ListOptions ListOptions { get; set; }

        public override void Init(TagHelperContext context) {
            RouteValues = ListOptions.ToDictionary(new { sort = SortBy.Metadata.PropertyName }).AsRouteValues().ToDictionary(x => x.Key, x => x.Value); // this last thing is a problem
            base.Init(context);
        }

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
