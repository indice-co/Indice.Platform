using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Indice.Features.Messages.Tests.Security;

internal class DummyAuthHandler : AuthenticationHandler<DummyAuthOptions>
{
    public DummyAuthHandler(IOptionsMonitor<DummyAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, IDataProtectionProvider dataProtection)
        : base(options, logger, encoder) { }

    /// <summary>
    /// Searches the 'Authorization' header for a 'Bearer' token. If the 'Bearer' token is found, it is validated using <see cref="TokenValidationParameters"/> set in the options.
    /// </summary>
    /// <returns></returns>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
        ClaimsPrincipal principal = Options.CreatePrincipal();
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, DummyAuthDefaults.AuthenticationScheme)));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties) {
        var authResult = await HandleAuthenticateOnceSafeAsync();

        if (authResult.Succeeded) {
            return;
        }

        Response.StatusCode = 401;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) {
        return base.HandleForbiddenAsync(properties);
    }
}
