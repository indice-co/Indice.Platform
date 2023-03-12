using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace ResourceOwnerPasswordFlow.Configuration;

internal class ConfigureRopfCookie : IConfigureNamedOptions<CookieAuthenticationOptions>
{
    public void Configure(string name, CookieAuthenticationOptions options) {
        if (name == Startup.CookieScheme) { }
    }

    public void Configure(CookieAuthenticationOptions options) => Configure(Options.DefaultName, options);
}
