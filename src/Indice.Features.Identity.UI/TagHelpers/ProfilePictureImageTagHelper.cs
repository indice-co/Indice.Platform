using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using IdentityServer4.Extensions;
using Indice.Security;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.TagHelpers;

/// <summary>
/// <see cref="ITagHelper"/> implementation targeting &lt;img&gt; elements that will calculate a users profile image url.
/// </summary>
/// <remarks>
/// The tag helper won't process for cases with just the 'src' attribute.
/// </remarks>
[HtmlTargetElement(
    "img",
    Attributes = ProfilePictureAttributeName,
    TagStructure = TagStructure.WithoutEndTag)]
public class ProfilePictureImageTagHelper : UrlResolutionTagHelper
{
    private const string ProfilePictureAttributeName = "profile-pic";
    private const string ProfilePictureIdAttributeName = "profile-pic-id";
    //private const string ProfilePictureSizeAttributeName = "profile-pic-size";
    private const string ProfileDisplayNameAttributeName = "profile-display-name";
    private readonly IOptions<IdentityUIOptions> _UiOptions;

    /// <summary>
    /// Creates a new <see cref="ProfilePictureImageTagHelper"/>.
    /// </summary>
    /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/> to use.</param>
    /// <param name="urlHelperFactory">The <see cref="IUrlHelperFactory"/>.</param>
    /// <param name="uiOptions">The <see cref="IdentityUIOptions"/> to use for image color fallback</param>
    public ProfilePictureImageTagHelper(
        HtmlEncoder htmlEncoder,
        IUrlHelperFactory urlHelperFactory,
        IOptions<IdentityUIOptions> uiOptions)
        : base(urlHelperFactory, htmlEncoder) {
        _UiOptions = uiOptions;
    }

    /// <inheritdoc />
    public override int Order => -1000;

    /// <summary>
    /// The user id.
    /// </summary>
    /// <remarks>
    /// Optional for authenticated users
    /// </remarks>
    [HtmlAttributeName(ProfilePictureIdAttributeName)]
    public string? PictureId { get; set; }

    /// <summary>
    /// Users display name. Will be used for the fallback url.
    /// </summary>
    /// <remarks>
    /// Optional for authenticated users
    /// </remarks>
    [HtmlAttributeName(ProfileDisplayNameAttributeName)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Users picture requested size.
    /// </summary>
    /// <remarks>
    /// Optional will default to maximum available size
    /// </remarks>
    [HtmlAttributeName(ProfilePictureAttributeName)]
    public int? Size { get; set; }

    private ClaimsPrincipal User => ViewContext.HttpContext.User;
    private bool CanRender => !string.IsNullOrWhiteSpace(PictureId) || User.IsAuthenticated();

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output) {
        if (context == null) {
            throw new ArgumentNullException(nameof(context));
        }

        if (output == null) {
            throw new ArgumentNullException(nameof(output));
        }
        if (output.Attributes.ContainsName("src") || !CanRender) {
            return;
        }
       
        var url = new StringBuilder("~/");
        if (User.IsAuthenticated()) {
            url.Append("api/my/account/picture");
            if (Size.HasValue) {
                url.Append($"/{Size}");
            }
            var fallBackUrl = new Uri($"/avatar/{User.FindDisplayName()}/{Size ?? 128}/{_UiOptions.Value.AvatarColorHex}.png", UriKind.RelativeOrAbsolute);
            url.Append($"?d={UriHelper.Encode(fallBackUrl)}");
        } else {
            url.Append($"pictures/{PictureId}");
            if (Size.HasValue) {
                url.Append($"/{Size}");
            }
            var fallBackUrl = new Uri($"/avatar/{User.FindDisplayName()}/{Size ?? 128}/{_UiOptions.Value.AvatarColorHex}.png", UriKind.RelativeOrAbsolute);
            url.Append($"?d={UriHelper.Encode(fallBackUrl)}");
        }
        output.Attributes.Add("src", url.ToString());
        if (!output.Attributes.ContainsName("alt")) {
            output.Attributes.Add("alt", $"{DisplayName ?? User.FindDisplayName()} avatar");
        }
        ProcessUrlAttribute("src", output);
    }
}
