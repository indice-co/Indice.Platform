﻿using System.Security.Claims;
using Indice.Security;

namespace Indice.Features.Identity.UI;

/// <summary>Configuration options for Identity UI.</summary>
public class IdentityUIOptions
{
    /// <summary>
    /// The copywrite year from. Usualy found on UI footer
    /// </summary>
    public int CopyYear { get; set; } = DateTime.Now.Year;

    /// <summary>This is the slogan that will be displayed in the home page.</summary>
    /// <remarks>
    /// There are already translations for the following: 
    /// <list>
    /// <item>
    ///     Welcome to our Digital Services &lt;strong&gt;Portal&lt;/strong&gt; of {0}
    /// </item>
    /// <item>
    ///     &lt;strong>Welcome&lt;/strong&gt;&lt;br&gt; to our Digital Services Portal of {0}
    /// </item>
    /// </list>
    /// </remarks>
    public string HomePageSlogan { get; set; } = "Welcome to our Digital Services <strong>Portal</strong> of {0}";
    /// <summary>An absolute URL to the <strong>terms and conditions</strong> web page. Use it when this page is located to (or shared with) an external website.</summary>
    /// <remarks>If left null the <strong>./legal/terms.md</strong> will be used. If populated it will do a redirect to this URL</remarks>
    public string? TermsUrl { get; set; }
    /// <summary>An absolute URL to the <strong>privacy</strong> web page. Use it when this page is located to (or shared with) an external website.</summary>
    /// <remarks>If left null the <strong>./legal/privacy.md</strong> will be used. If populated it will do a redirect to this URL</remarks>
    public string? PrivacyUrl { get; set; }
    /// <summary>An absolute URL to the <strong>Contact us</strong> web page. Use it when this page is located to (or shared with) an external website.</summary>
    /// <remarks>If left null the <strong>Contact Us</strong> link in the footer will disappear. If populated it will do a redirect to this URL. By default it is empty</remarks>
    public string? ContactUsUrl { get; set; }
    /// <summary>The absolute page path (not the url) that points to a custom on boarding process start. For example <strong>/OnBoarding/Welcome</strong> would be the route pointing to a page under the physical path: <strong>/Pages/OnBoarding/Welcome.cshtml</strong></summary>
    /// <remarks>This will replace all register links pointing to the native register page <strong>/Register</strong> but will not shut down native registration. In order to shut down registration use <seealso cref="EnableRegisterPage"/>.</remarks>
    public string OnBoardingPage { get; set; } = "/Register";
    /// <summary>True if <see cref="OnBoardingPage"/> is not the default. Used to determine where <strong>/register</strong> links will be pointing.</summary>
    public bool HasCustomOnBoarding => !"/Register".Equals(OnBoardingPage);
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
    /// <summary>Overrides the default static file middleware and adds required assets as embedded resources.</summary>
    public bool OverrideDefaultStaticFileMiddleware { get; set; } = true;
    /// <summary>Stores the calling code along with the phone number.</summary>
    public bool EnablePhoneNumberCallingCodes { get; set; } = false;
    /// <summary>
    /// Used with <see cref="Indice.Globalization.PhoneNumber"/> instances to convert to predictable string for storage.
    /// </summary>
    public string PhoneNumberStoreFormat => EnablePhoneNumberCallingCodes ? "G" : "N";

    /// <summary>Services shown in the homepage.</summary>
    public List<HomePageLink> HomepageLinks { get; } = new List<HomePageLink>() {
        new HomePageLink("Admin","~/admin", CssClass:"admin", VisibilityPredicate: user => user.IsAdmin())
    };
    /// <summary>
    /// Should show the Add Email page before sending the confirmation email prompt (in case of pernding confirmation login) or Confirm emai immediately.
    /// </summary>
    /// <remarks>Useful when user store is from a migrated database and we need to force users to add an email where an email is not present. Defaults to true.</remarks>
    public bool ShowAddEmailPrompt { get; set; } = true;

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


