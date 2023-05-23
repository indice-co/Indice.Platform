using IdentityServer4.Services;
using Indice.AspNetCore.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Pages;

/// <summary>Page model for the logged out screen.</summary>
[SecurityHeaders]
[IdentityUI(typeof(LoggedOutModel))]
public abstract class BaseLoggedOutModel : BasePageModel
{
    /// <summary>Creates a new instance of <see cref="BaseLoggedOutModel"/> class.</summary>
    /// <param name="interaction">Provide services be used by the user interface to communicate with IdentityServer.</param>
    /// <param name="identityUiOptions">Configuration options for Identity UI.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public BaseLoggedOutModel(
        IIdentityServerInteractionService interaction,
        IOptions<IdentityUIOptions> identityUiOptions
    ) {
        Interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
        IdentityUIOptions = identityUiOptions?.Value ?? throw new ArgumentNullException(nameof(identityUiOptions));
    }

    /// <summary>Provide services be used by the user interface to communicate with IdentityServer.</summary>
    protected IIdentityServerInteractionService Interaction { get; }

    /// <summary>Setting to skip the logged out page.</summary>
    public bool AutomaticRedirectAfterSignOut { get; set; } = false;
    /// <summary>The client id.</summary>
    public string? ClientId { get; set; }
    /// <summary>The name associated with the client.</summary>
    public string? ClientName { get; set; }
    /// <summary>The configured post logout URL for the client being logged out.</summary>
    public string? PostLogoutRedirectUri { get; set; }
    /// <summary>The sign out IFrame URL.</summary>
    public string? SignOutIframeUrl { get; set; }
    /// <summary>Configuration options for Identity UI.</summary>
    protected IdentityUIOptions IdentityUIOptions { get; set; }

    /// <summary>Logged out page GET handler.</summary>
    public virtual async Task<IActionResult> OnGetAsync(string logoutId) {
        // Get context information (client name, post logout redirect URI and iframe for federated sign out).
        var logout = await Interaction.GetLogoutContextAsync(logoutId);
        AutomaticRedirectAfterSignOut = IdentityUIOptions.AutomaticRedirectAfterSignOut;
        ClientId = logout?.ClientId;
        ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName;
        PostLogoutRedirectUri = logout?.PostLogoutRedirectUri;
        SignOutIframeUrl = logout?.SignOutIFrameUrl;
        return Page();
    }
}

internal class LoggedOutModel : BaseLoggedOutModel
{
    public LoggedOutModel(
        IIdentityServerInteractionService interaction,
        IOptions<IdentityUIOptions> identityUiOptions
    ) : base(interaction, identityUiOptions) { }
}
