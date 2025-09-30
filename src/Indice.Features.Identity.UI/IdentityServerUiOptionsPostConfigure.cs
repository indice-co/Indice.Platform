#if NET9_0_OR_GREATER
using Duende.IdentityServer.Configuration;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI;

/// <summary>
/// Provides post-configuration for <see cref="IdentityServerOptions"/> 
/// that requires dependency-injected services (e.g. <see cref="LinkGenerator"/>).
/// This runs after the options are bound and allows customizing properties 
/// such as IssuerUri, endpoints, or URLs based on application state.
/// </summary>
public class IdentityServerOptionsPostConfigure(IOptions<IdentityUIOptions> identityUiOptions) : IPostConfigureOptions<IdentityServerOptions>
{
    private readonly IdentityUIOptions _identityUiOptions = identityUiOptions.Value;

    ///<inheritdoc/>
    public void PostConfigure(string? name, IdentityServerOptions options) {
        options.KeyManagement.Enabled = false;
        if (!string.IsNullOrEmpty(_identityUiOptions.OnBoardingPage)) {
            options.UserInteraction.CreateAccountUrl = _identityUiOptions.OnBoardingPage.ToLowerInvariant();
        }
    }
}
#endif