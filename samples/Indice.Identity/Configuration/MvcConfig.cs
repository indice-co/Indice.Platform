using FluentValidation.AspNetCore;
using Indice.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// MVC configuration.
    /// </summary>
    public static class MvcConfig
    {
        /// <summary>
        /// Configures the core MVC services for the application.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public static IMvcBuilder AddMvcConfig(this IServiceCollection services) {
            return services.AddMvc()
                           .SetCompatibilityVersion(CompatibilityVersion.Latest)
                           .ConfigureApiBehaviorOptions(options => {
                               options.ClientErrorMapping[400].Link = "https://httpstatuses.com/400";
                               options.ClientErrorMapping[401].Link = "https://httpstatuses.com/401";
                               options.ClientErrorMapping[403].Link = "https://httpstatuses.com/403";
                               options.ClientErrorMapping[404].Link = "https://httpstatuses.com/404";
                               options.ClientErrorMapping.Add(405, new ClientErrorData {
                                   Link = "https://httpstatuses.com/405",
                                   Title = "Method Not Allowed"
                               });
                               options.ClientErrorMapping[406].Link = "https://httpstatuses.com/406";
                               options.ClientErrorMapping[409].Link = "https://httpstatuses.com/409";
                               options.ClientErrorMapping.Add(429, new ClientErrorData {
                                   Link = "https://httpstatuses.com/429",
                                   Title = "Too Many Requests"
                               });
                               options.ClientErrorMapping.Add(500, new ClientErrorData {
                                   Link = "https://httpstatuses.com/500",
                                   Title = "Internal Server Error"
                               });
                           })
                           .AddCookieTempDataProvider()
                           .AddMvcOptions(options => {
                               options.FormatterMappings.SetMediaTypeMappingForFormat("json", "application/json");
                               options.FormatterMappings.SetMediaTypeMappingForFormat("pdf", "application/pdf");
                               options.FormatterMappings.SetMediaTypeMappingForFormat("html", "text/html");
                               options.FormatterMappings.SetMediaTypeMappingForFormat("docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                               options.FormatterMappings.SetMediaTypeMappingForFormat("xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                           })
                           .AddJsonOptions(options => {
                               options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver {
                                   NamingStrategy = new CamelCaseNamingStrategy {
                                       ProcessDictionaryKeys = false,
                                       OverrideSpecifiedNames = false
                                   }
                               };
                               options.SerializerSettings.Converters.Add(new StringEnumConverter());
                               options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                               options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Include;
                           })
                           .AddFluentValidation(options => {
                               options.RegisterValidatorsFromAssemblyContaining<Startup>();
                               options.ConfigureClientsideValidation();
                               options.RunDefaultMvcValidationAfterFluentValidationExecutes = false;
                           });
        }
    }
}
