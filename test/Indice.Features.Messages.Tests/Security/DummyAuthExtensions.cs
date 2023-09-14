using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Indice.Features.Messages.Tests.Security;
public static class DummyAuthExtensions
{
    public static AuthenticationBuilder AddDummy(this AuthenticationBuilder builder, Func<ClaimsPrincipal> CreateCrincipal)
        => builder.AddScheme<DummyAuthOptions, DummyAuthHandler>(DummyAuthDefaults.AuthenticationScheme, "Dummy Auth", o => o.CreatePrincipal = CreateCrincipal);
}
