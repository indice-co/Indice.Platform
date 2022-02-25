using System.Text.Json;
using System.Text.Json.Serialization;
using IdentityModel;
using Indice.AspNetCore.Features.Campaigns;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MvcConfig
    {
        public static IMvcBuilder AddMvcConfig(this IServiceCollection services, IConfiguration configuration) {
            var mvcBuilder = services.AddControllers()
                                     .AddCampaignsApiEndpoints(options => {
                                         options.ApiPrefix = "api";
                                         options.ConfigureDbContext = builder => builder.UseSqlServer(configuration.GetConnectionString("CampaignsDb"));
                                         options.DatabaseSchema = "cmp";
                                         options.RequiredScope = $"backoffice:{CampaignsApi.Scope}";
                                         options.UserClaimType = JwtClaimTypes.Subject;
                                         options.UseFilesLocal(fileOptions => fileOptions.Path = "uploads");
                                         options.UseEventDispatcherAzure((serviceProvider, eventDispatcherOptions) => { });
                                     })
                                     .AddSettingsApiEndpoints(options => {
                                         options.ApiPrefix = "api";
                                         options.RequiredScope = "backoffice";
                                         options.ConfigureDbContext = builder => builder.UseSqlServer(configuration.GetConnectionString("SettingsDb"));
                                     })
                                     .AddJsonOptions(options => {
                                         options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                                         options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                                         options.JsonSerializerOptions.WriteIndented = true;
                                     });
            return mvcBuilder;
        }
    }
}
