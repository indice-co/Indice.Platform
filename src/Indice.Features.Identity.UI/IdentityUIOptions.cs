using System.Security.Claims;
using Indice.Security;

namespace Indice.Features.Identity.UI;

/// <summary>Configuration options for Identity UI.</summary>
public class IdentityUIOptions
{
    /// <summary>Affects the ui framework that the views will be based upon. Defaults to Bootstrap 5.</summary>
    public string? UiFramework { get; set; }
    /// <summary>An absolute URL to the <strong>terms and conditions</strong> web page. Use it when this page is located to (or shared with) an external website.</summary>
    /// <remarks>If left null the <strong>./legal/terms.md</strong> will be used. If populated it will do a redirect to this URL</remarks>
    public string? TermsUrl { get; set; }
    /// <summary>An absolute URL to the <strong>privacy</strong> web page. Use it when this page is located to (or shared with) an external website.</summary>
    /// <remarks>If left null the <strong>./legal/privacy.md</strong> will be used. If populated it will do a redirect to this URL</remarks>
    public string? PrivacyUrl { get; set; }
    /// <summary>Controls whether an external Identity user will go through the associate screen or not.</summary>
    public bool AutoProvisionExternalUsers { get; set; } = true;
    /// <summary>Controls whether an external identity user be associated to an existing one using the email account.</summary>
    public bool AutoAssociateExternalUsers { get; set; } = true;
    /// <summary>Controls whether The self service /register page is accessible.</summary>
    public bool EnableRegisterPage { get; set; } = true;
    /// <summary>Controls whether The self service /forgot-password page is accessible.</summary>
    public bool EnableForgotPasswordPage { get; set; } = true;
    /// <summary>Allow remember login.</summary>
    public bool AllowRememberLogin { get; set; } = true;
    /// <summary>Enables local logins (if false only external provider list will be available).</summary>
    public bool EnableLocalLogin { get; set; } = true;
    /// <summary>RGB color to be used with avatar endpoints to render the user avatar background with initials.</summary>
    public string AvatarColorHex { get; set; } = "1abc9c";
    /// <summary>RGB color to be used with email default templates for links.</summary>
    public string EmailLinkColorHex { get; set; } = "1abc9c";
    /// <summary>RGB color to be used with email default templates for links.</summary>
    public string HtmlBodyBackgroundCssClass { get; set; } = "gradient-bg";
    /// <summary>Contains additional valid return URLs. It's used in the login page.</summary>
    public HashSet<string> ValidReturnUrls { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    /// <summary>Remember me duration.</summary>
    public TimeSpan RememberMeLoginDuration { get; set; } = TimeSpan.FromDays(30);
    /// <summary>Should show the logout prompt or logout immediately.</summary>
    public bool ShowLogoutPrompt { get; set; } = true;
    /// <summary>Automatic redirect after sign out.</summary>
    public bool AutomaticRedirectAfterSignOut { get; set; } = false;
    /// <summary>The two-letter country codes to allow phone numbers from.</summary>
    public string[]? PhoneCountries { get; set; }

    /// <summary>Services shown in the homepage.</summary>
    public List<HomePageLink> HomepageLinks { get; } = new List<HomePageLink>() {
        new HomePageLink("Admin","~/admin", CssClass:"admin", VisibilityPredicate: user => user.IsAdmin())
    };

    /// <summary>Adds a homepage link to the a service definition cards.</summary>
    /// <param name="displayName">The label.</param>
    /// <param name="link">The hyperlink that the service will redirect.</param>
    /// <param name="cssClass">The css class for the card.</param>
    /// <param name="imageSrc">A src url for the image tag.</param>
    /// <param name="visibilityPredicate">The visibility predicate.</param>
    public IdentityUIOptions AddHomepageLink(string displayName, string link, string? cssClass = null, string? imageSrc = null, Predicate<ClaimsPrincipal>? visibilityPredicate = null) {
        HomepageLinks.Add(new HomePageLink(displayName, link, cssClass, imageSrc, visibilityPredicate));
        return this;
    }

    /// <summary>Check against <see cref="ValidReturnUrls"/> for any valid uri.</summary>
    /// <returns>True if contained in the list.</returns>
    public bool IsValidReturnUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out var _) && ValidReturnUrls.Contains(url);

    /// <summary>Gateway service definition. Will be visible in the homepage.</summary>
    /// <param name="DisplayName">The label.</param>
    /// <param name="Link">The hyperlink that the service will redirect.</param>
    /// <param name="CssClass">The css class for the card.</param>
    /// <param name="ImageSrc">A src url for the image tag.</param>
    /// <param name="VisibilityPredicate">The visibility predicate.</param>
    public record HomePageLink(string DisplayName, string Link, string? CssClass = null, string? ImageSrc = null, Predicate<ClaimsPrincipal>? VisibilityPredicate = null);
}


