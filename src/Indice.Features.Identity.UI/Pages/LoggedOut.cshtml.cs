using IdentityServer4.Services;
using Indice.AspNetCore.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the logged out screen.</summary>
[SecurityHeaders]
[IdentityUI(typeof(LoggedOutModel))]
public abstract class BaseLoggedOutModel : BasePageModel
{
    private readonly IIdentityServerInteractionService _interaction;

    /// <summary>Creates a new instance of <see cref="BaseLoggedOutModel"/> class.</summary>
    /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseLoggedOutModel(IIdentityServerInteractionService interaction) {
        _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
    }

    /// <summary>Setting to skip the logged out page.</summary>
    public bool AutomaticRedirectAfterSignOut { get; set; } = false;
    /// <summary>The client id</summary>
    public string? ClientId { get; set; }
    /// <summary>The name associated with the client</summary>
    public string? ClientName { get; set; }
    /// <summary>The configured post logout URL for the client being logged out.</summary>
    public string? PostLogoutRedirectUri { get; set; }
    /// <summary>the SignoutIFrameurl</summary>
    public string? SignOutIframeUrl { get; set; }

    /// <summary>Logged out page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync(string logoutId) {
        // Get context information (client name, post logout redirect URI and iframe for federated sign out).
        var logout = await _interaction.GetLogoutContextAsync(logoutId);
        AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut;
        ClientId = logout?.ClientId;
        ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName;
        PostLogoutRedirectUri = logout?.PostLogoutRedirectUri;
        SignOutIframeUrl = logout?.SignOutIFrameUrl;
        return Page();
    }
}

internal class LoggedOutModel : BaseLoggedOutModel
{
    public LoggedOutModel(IIdentityServerInteractionService interaction) : base(interaction) { }
}
