using Indice.Features.Identity.Server.Manager.Models;
using Indice.Types;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Identity.Server.Manager;

internal static class LookupHandlers
{
    internal static async Task<Ok<ResultSet<ExternalProvider>>> GetExternalProviders(IAuthenticationSchemeProvider schemeProvider) {
        var providers = (await schemeProvider.GetAllSchemesAsync())
            .Where(x => x.DisplayName != null)
            .Select(x => new ExternalProvider {
                DisplayName = x.DisplayName ?? x.Name,
                AuthenticationScheme = x.Name
            })
            .ToResultSet();
        return TypedResults.Ok(providers);
    }
}
