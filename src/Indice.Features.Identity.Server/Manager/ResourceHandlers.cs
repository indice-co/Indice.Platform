using System.Security.Claims;
using System.ServiceModel.Syndication;
using System.Xml;
using Humanizer;
using IdentityServer4.EntityFramework.Entities;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Server.Manager.Models;
using Indice.Features.Identity.Server.Options;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

namespace Indice.Features.Identity.Server.Manager;
internal static class ResourceHandlers
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


    /// <summary>Adds translations to an <see cref="ApiScope"/>.</summary>
    /// <remarks>If the parameter translations is null, string.Empty will be persisted.</remarks>
    /// <param name="apiScope">The <see cref="ApiScope"/>.</param>
    /// <param name="translations">The JSON string with the translations</param>
    private static void AddTranslationsToApiScope(ApiScope apiScope, string translations) {
        apiScope.Properties ??= new List<ApiScopeProperty>();
        apiScope.Properties.Add(new ApiScopeProperty {
            Key = ClientPropertyKeys.Translation,
            Value = translations ?? string.Empty,
            Scope = apiScope
        });
    }
}
