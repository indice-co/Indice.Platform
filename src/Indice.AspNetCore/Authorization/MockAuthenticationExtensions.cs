using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Indice.AspNetCore.Authorization;
/// <summary>
/// Mock Authorization Extensions.
/// </summary>
public static class MockAuthenticationExtensions
{
    /// <summary>
    /// Adds Mock Authorization scheme to the <seealso cref="AuthenticationBuilder"/>
    /// </summary>
    /// <param name="builder">The builder to configure</param>
    /// <param name="createPrincipal">A factory function that returns the <see cref="ClaimsPrincipal"/> to be set as the Current user.</param>
    /// <returns></returns>
    public static AuthenticationBuilder AddMock(this AuthenticationBuilder builder, Func<ClaimsPrincipal> createPrincipal)
        => AddMock(builder, MockAuthenticationDefaults.AuthenticationScheme, "Mock Auth", createPrincipal);


    /// <summary>
    /// Adds Mock Authorization scheme to the <seealso cref="AuthenticationBuilder"/>
    /// </summary>
    /// <param name="builder">The builder to configure</param>
    /// <param name="authenticationScheme">The name of this scheme.</param>
    /// <param name="displayName">The display name of this scheme.</param>
    /// <param name="createPrincipal">A factory function that returns the <see cref="ClaimsPrincipal"/> to be set as the Current user.</param>
    /// <returns></returns>
    public static AuthenticationBuilder AddMock(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Func<ClaimsPrincipal> createPrincipal)
        => builder.AddScheme<MockAuthenticationOptions, MockAuthenticationHandler>(authenticationScheme, displayName, o => o.CreatePrincipal = createPrincipal);
}
