using System.Text.Json;
using System.Text.Json.Serialization;
using IdentityModel;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Worker.Azure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MvcConfig
    {
        public static IMvcBuilder AddMvcConfig(this IServiceCollection services, IConfiguration configuration) {
            var mvcBuilder = services
                .AddControllers()
                .AddMessageEndpoints(options => {
                    options.ApiPrefix = "api";
                    options.ConfigureDbContext = builder => builder.UseSqlServer(configuration.GetConnectionString("MessagesDb"));
                    options.DatabaseSchema = "cmp";
                    options.RequiredScope = $"backoffice:{MessagesApi.Scope}";
                    options.UserClaimType = JwtClaimTypes.Subject;
                    options.InboxGroupName = "lookups";
                    options.UseFilesLocal(fileOptions => fileOptions.Path = "uploads");
                    //options.UseEventDispatcherHosting();
                    options.UseEventDispatcherAzure();
                })
                .AddSettingsApiEndpoints(options => {
                    options.ApiPrefix = "api";
                    options.RequiredScope = "backoffice";
                    options.AuthenticationSchemes = new[] { JwtBearerDefaults.AuthenticationScheme };
                    options.ConfigureDbContext = builder => builder.UseSqlServer(configuration.GetConnectionString("SettingsDb"));
                })
                .AddJsonOptions(options => {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    options.JsonSerializerOptions.WriteIndented = true;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            return mvcBuilder;
        }
    }
}
