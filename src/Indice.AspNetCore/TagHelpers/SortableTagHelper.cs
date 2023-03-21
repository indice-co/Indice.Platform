using Indice.Types;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;

namespace Indice.AspNetCore.TagHelpers;

/// <summary>Tag helper that offers the ability to generate table header links used along with <see cref="ListOptions"/>.</summary>
[HtmlTargetElement("th", Attributes = "[class^=" + SortableClassName + "]")]
[HtmlTargetElement("th", Attributes = "[sort-by]")]
public class SortableTagHelper : TagHelper
{
    private const string SortableClassName = "sortable";
    private const string AttributePrefix = "sort";
    private readonly IHtmlGenerator Generator;

    /// <summary>Create a new instance of <see cref="SortableTagHelper"/>.</summary>
    /// <param name="generator">Contract for a service supporting <see cref="IHtmlHelper"/> and <see cref="ITagHelper"/> implementations.</param>
    public SortableTagHelper(IHtmlGenerator generator) {
        Generator = generator ?? throw new ArgumentNullException(nameof(generator));
    }

    /// <summary>Context for view execution.</summary>
    [ViewContext]
    public ViewContext ViewContext { get; set; }
    /// <summary>List params used to navigate through collections.</summary>
    [HtmlAttributeName("list-options")]
    public ListOptions Options { get; set; }
    /// <summary>A model expression used in for sorting the table collection.</summary>
    public ModelExpression SortBy { get; set; }

    /// <summary>Asynchronously executes the <see cref="TagHelper"/> with the given context and output.</summary>
    /// <param name="context">Contains information associated with the current HTML tag.</param>
    /// <param name="output">A stateful HTML element used to generate an HTML tag.</param>
    public async override Task ProcessAsync(TagHelperContext context, TagHelperOutput output) {
        if (SortBy == null) {
            throw new InvalidOperationException($"A {nameof(SortBy)} expression must be provided.");
        }
        if (Options == null) {
            throw new InvalidOperationException($"The {nameof(ListOptions)} must be provided.");
        }
        var sortBy = SortBy.Metadata.PropertyName.ToLowerInvariant();
        var sortings = Options.GetSortings().ToDictionary(s => s.Path.ToLowerInvariant());
        var sortByClass = sortings.ContainsKey(sortBy) ? $"{AttributePrefix}-{sortings[sortBy].Direction.ToLower() }" : null;
        if (output.Attributes.TryGetAttribute("class", out var css)) {
            var classesList = css.Value.ToString().Split(' ').Where(name => !name.StartsWith("sort-", StringComparison.Ordinal)).ToList();
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
            var anchorTag = Generator.GenerateActionLink(ViewContext, string.Empty, null, null, null, null, null, routeValues, null);
            var spanTag = new TagBuilder("span");
            spanTag.InnerHtml.Append(SortBy.Metadata.GetDisplayName());
            anchorTag.InnerHtml.AppendHtml(spanTag);
            container.InnerHtml.AppendHtml(anchorTag);
            output.Content.SetHtmlContent(container);
        }
    }
}
