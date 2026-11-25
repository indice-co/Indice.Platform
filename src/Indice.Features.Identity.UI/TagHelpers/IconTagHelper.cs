using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Indice.Features.Identity.UI.TagHelpers;

/// <summary>
/// <see cref="ITagHelper"/> implementation that renders a <see href="https://lucide.dev/icons">Lucide</see> icon from the Identity UI sprite.
/// </summary>
/// <remarks>
/// <c>&lt;icon name="mail" class="extra" /&gt;</c> renders <c>&lt;svg class="icon extra" aria-hidden="true" focusable="false"&gt;&lt;use href="/lib/lucide/sprite.svg?v=…#mail"&gt;&lt;/use&gt;&lt;/svg&gt;</c>.
/// Icons are decorative by default; set an <c>aria-label</c> to expose the icon to assistive technologies (it is then rendered with <c>role="img"</c>).
/// Icons inherit the current text color and are sized to <c>1em</c> by the <c>.icon</c> class.
/// </remarks>
[HtmlTargetElement(TagName, Attributes = NameAttributeName, TagStructure = TagStructure.WithoutEndTag)]
public class IconTagHelper : TagHelper
{
    private const string TagName = "icon";
    private const string NameAttributeName = "name";

    /// <summary>The Lucide icon name (i.e. <c>mail</c>, <c>shield-check</c>).</summary>
    [HtmlAttributeName(NameAttributeName)]
    public string Name { get; set; } = string.Empty;

    /// <summary>The current view context.</summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output) {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);
        if (string.IsNullOrWhiteSpace(Name)) {
            output.SuppressOutput();
            return;
        }
        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("icon", HtmlEncoder.Default);
        if (output.Attributes.ContainsName("aria-label")) {
            output.Attributes.SetAttribute("role", "img");
        } else {
            output.Attributes.SetAttribute("aria-hidden", "true");
        }
        output.Attributes.SetAttribute("focusable", "false");
        output.Attributes.SetAttribute("width", "1em");
        output.Attributes.SetAttribute("height", "1em");
        var href = $"{IdentityUIIcons.GetSpriteHref(ViewContext)}#{Name.Trim()}";
        output.Content.SetHtmlContent($"<use href=\"{HtmlEncoder.Default.Encode(href)}\"></use>");
    }
}
