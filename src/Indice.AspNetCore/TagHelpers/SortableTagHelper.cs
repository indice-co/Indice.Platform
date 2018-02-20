using Indice.AspNetCore.Types;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Indice.AspNetCore.TagHelpers
{
    [HtmlTargetElement("th", Attributes = "[class^=" + SortableClassName + "]")]
    [HtmlTargetElement("th", Attributes = "[sort-by]")]
    public class SortableTagHelper : TagHelper
    {
        const string SortableClassName = "sortable";
        const string AttributePrefix = "sort";

        protected readonly IHtmlGenerator Generator;

        public SortableTagHelper(IHtmlGenerator generator) {
            if (generator == null) {
                throw new ArgumentNullException(nameof(generator));
            }
            Generator = generator;
        }


        [ViewContext]
        public ViewContext ViewContext { get; set; }


        [HtmlAttributeName("list-options")]
        public ListOptions Options { get; set; }

        public ModelExpression SortBy { get; set; }

        public async override Task ProcessAsync(TagHelperContext context, TagHelperOutput output) {
            if (SortBy == null) {
                throw new InvalidOperationException($"A {nameof(SortBy)} expression must be provided.");
            }
            if (Options == null) {
                throw new InvalidOperationException($"The list options must be provided.");
            }
            var css = default(TagHelperAttribute);
            var sortBy = SortBy.Metadata.PropertyName.ToLowerInvariant();
            var sortings = Options.GetSortings().ToDictionary(s => s.Path.ToLowerInvariant());
            var sortByClass = sortings.ContainsKey(sortBy) ?
                              $"{AttributePrefix}-{sortings[sortBy].Direction.ToLower() }" : null;
            if (output.Attributes.TryGetAttribute("class", out css)) {
                var classesList = css.Value
                                     .ToString().Split(' ')
                                     .Where(name => !name.StartsWith("sort-", StringComparison.Ordinal))
                                     .ToList();
                if (!classesList.Contains(SortableClassName)) {
                    classesList.Insert(0, SortableClassName);
                }
                if (!string.IsNullOrEmpty(sortByClass)) {
                    classesList.Add(sortByClass);
                }
                output.Attributes.SetAttribute("class", string.Join(" ", classesList));
            } else {
                output.Attributes.SetAttribute("class", $"{SortableClassName} {sortByClass}".TrimEnd());
            }

            var childContent = await output.GetChildContentAsync();
            if (childContent.IsEmptyOrWhiteSpace) {
                var values = Options.ToDictionary(new { Sort = SortBy.Metadata.PropertyName }).AsRouteValues();
                var routeValues = new RouteValueDictionary();
                foreach (var item in values) {
                    routeValues.Add(item.Key, item.Value);
                }
                var container = new TagBuilder("div");
                var anchorTag = Generator.GenerateActionLink(ViewContext, 
                                                             string.Empty, null, null, null, null, null,
                                                             routeValues,
                                                             null);
                var spanTag = new TagBuilder("span");
                spanTag.InnerHtml.Append(SortBy.Metadata.GetDisplayName());
                anchorTag.InnerHtml.AppendHtml(spanTag);
                container.InnerHtml.AppendHtml(anchorTag);
                output.Content.SetHtmlContent(container);
            }
        }

    }
}
