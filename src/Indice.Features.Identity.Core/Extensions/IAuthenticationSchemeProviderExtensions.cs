using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>Extension methods on <see cref="IAuthenticationSchemeProvider"/> interface.</summary>
public static class IAuthenticationSchemeProviderExtensions
{
    /// <summary>Gets the list of all external authentication schemes.</summary>
    /// <param name="schemeProvider">Responsible for managing what authenticationSchemes are supported.</param>
    public static async Task<IEnumerable<AuthenticationScheme>> GetExternalSchemesAsync(this IAuthenticationSchemeProvider schemeProvider) {
        var allSchemes = await schemeProvider.GetAllSchemesAsync();
        var externalSchemes = allSchemes.Where(scheme => typeof(OpenIdConnectHandler).IsAssignableFrom(scheme.HandlerType) || typeof(OAuthHandler<>).IsAssignableFrom(scheme.HandlerType.BaseType?.GetGenericTypeDefinition()));
        return externalSchemes;
    }
}
