using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Authorization;
/// <summary>
/// Configuration options to be used by the <see cref="MockAuthenticationHandler"/>
/// </summary>
public class MockAuthenticationOptions : AuthenticationSchemeOptions
{

    /// <summary>
    /// Gets or sets the challenge to put in the "WWW-Authenticate" header.
    /// </summary>
    public string Challenge { get; set; } = MockAuthenticationDefaults.AuthenticationScheme;

    /// <summary>
    /// The factory function that will create the current user to be set to the <see cref="HttpContext"/>
    /// </summary>
    public Func<ClaimsPrincipal> CreatePrincipal { get; set; }
}
