using Microsoft.AspNetCore.Authentication;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Indice.AspNetCore.Authorization;

internal class MockAuthenticationHandler(IOptionsMonitor<MockAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<MockAuthenticationOptions>(options, logger, encoder)
{

    /// <summary>
    /// Searches the 'Authorization' header for a 'Bearer' token. If the 'Bearer' token is found, it is validated using <see cref="TokenValidationParameters"/> set in the options.
    /// </summary>
    /// <returns></returns>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
        var principal = Options.CreatePrincipal();
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, MockAuthenticationDefaults.AuthenticationScheme)));
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
