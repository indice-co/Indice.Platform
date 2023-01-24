using System.Text.Json;
using System.Text.Json.Serialization;
using IdentityModel;
using Indice.Features.Messages.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MvcConfig
    {
        public static IMvcBuilder AddMvcConfig(this IServiceCollection services, IConfiguration configuration) {
            var mvcBuilder = services
                .AddControllers()
                .AddMessageEndpoints(options => {
                    options.ApiPrefix = "api";
                    options.ConfigureDbContext = (serviceProvider, builder) => builder.UseSqlServer(configuration.GetConnectionString("MessagesDb"));
                    options.DatabaseSchema = "msg";
                    options.RequiredScope = $"backoffice:{MessagesApi.Scope}";
                    options.UserClaimType = JwtClaimTypes.Subject;
                    options.UseFilesAzure();
                    //options.UseFilesLocal(fileOptions => fileOptions.Path = "uploads");
                    options.UseEventDispatcherAzure();
                    //options.UseEventDispatcherHosting();
                    options.UseIdentityContactResolver(resolverOptions => {
                        resolverOptions.BaseAddress = new Uri(configuration["IdentityServer:BaseAddress"]);
                        resolverOptions.ClientId = configuration["IdentityServer:ClientId"];
                        resolverOptions.ClientSecret = configuration["IdentityServer:ClientSecret"];
                    });
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
                })
                .AddAvatars();
            return mvcBuilder;
        }
    }
}
