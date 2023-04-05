using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.AppSettings;
using Indice.Features.Identity.Server.AppSettings.Models;
using Indice.Features.Identity.Server.Options;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

/// <summary></summary>
public static class SettingsApi
{
    /// <summary></summary>
    /// <param name="routeBuilder">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static RouteGroupBuilder MapDatabaseSettingEndpoints(this IEndpointRouteBuilder routeBuilder) {
        var options = routeBuilder.GetEndpointOptions<SettingsApiOptions>();
        var group = routeBuilder
            .MapGroup($"{options.ApiPrefix}/app-settings")
            .WithTags("AppSettings")
            .WithGroupName("identity")
            .RequireAuthorization(policy => policy
               .AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
               .RequireAuthenticatedUser()
               .RequireAssertion(x => x.User.HasScope(options.ApiScope) && x.User.IsAdmin())
            )
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.WithOpenApi()
             .AddOpenApiSecurityRequirement("oauth2", options.ApiScope);
        // GET: /api/app-settings
        group.MapGet(string.Empty, SettingHandlers.GetSettings)
             .WithName(nameof(SettingHandlers.GetSettings))
             .WithSummary($"Returns a list of {nameof(AppSettingInfo)} objects containing the total number of application settings in the database and the data filtered according to the provided {nameof(AppSettingInfo)}.");
        // POST /api/app-settings/load
        group.MapPost("load", SettingHandlers.LoadFromAppSettingsJson)
             .WithName(nameof(SettingHandlers.LoadFromAppSettingsJson))
             .WithSummary("Loads the appsettings.json file and saves the configuration in the database.");
        // GET: /api/app-settings/{key}
        group.MapGet("{key}", SettingHandlers.GetSettingByKey)
             .WithName(nameof(SettingHandlers.GetSettingByKey))
             .WithSummary("Gets an application setting by it's key.");
        // POST: /api/app-settings
        group.MapPost(string.Empty, SettingHandlers.CreateSetting)
             .WithName(nameof(SettingHandlers.CreateSetting))
             .WithSummary("Creates a new application setting.")
             .WithParameterValidation<CreateAppSettingRequest>();
        // PUT: /api/app-settings/{key}
        group.MapPut("{key}", SettingHandlers.UpdateSetting)
             .WithName(nameof(SettingHandlers.UpdateSetting))
             .WithSummary("Updates an existing application setting.")
             .WithParameterValidation<UpdateAppSettingRequest>();
        // DELETE: /api/app-settings/{key}
        group.MapDelete("{key}", SettingHandlers.DeleteSetting)
             .WithName(nameof(SettingHandlers.DeleteSetting))
             .WithSummary("Permanently deletes an application setting.");
        return group;
    }
}
