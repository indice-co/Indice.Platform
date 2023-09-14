using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Indice.Features.Messages.Tests.Security;
public class DummyAuthOptions : AuthenticationSchemeOptions
{

    /// <summary>
    /// Gets or sets the challenge to put in the "WWW-Authenticate" header.
    /// </summary>
    public string Challenge { get; set; } = DummyAuthDefaults.AuthenticationScheme;

    public Func<ClaimsPrincipal> CreatePrincipal { get; set; }
}
