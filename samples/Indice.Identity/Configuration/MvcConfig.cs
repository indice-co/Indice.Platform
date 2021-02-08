using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using Indice.AspNetCore.Identity.Features;
using Indice.Identity;
using Indice.Identity.Services;
using Indice.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static IMvcBuilder AddMvcConfig(this IServiceCollection services, IConfiguration configuration) {
            return services.AddControllersWithViews()
                           .AddRazorRuntimeCompilation()
                           .AddTotp()
                           /*.AddPushNotifications(
                                options => {
                                    options.ConnectionString = configuration.GetConnectionString("PushNotificationsConnection");
                                    options.NotificationHubPath = configuration["PushNotifications:PushNotificationsHubPath"];
                                }
                            )*/
                           .AddIdentityServerApiEndpoints(options => {
                               // Configure the DbContext.
                               options.AddDbContext(identityOptions => {
                                   identityOptions.ConfigureDbContext = builder => {
                                       builder.UseSqlServer(configuration.GetConnectionString("IdentityDb"));
                                   };
                               });
                               // Enable events and register handlers.
                               options.CanRaiseEvents = true;
                               options.DisableCache = false;
                               options.AddEventHandler<ClientCreatedEvent, ClientCreatedEventHandler>();
                               options.AddEventHandler<UserEmailConfirmedEvent, UserEmailConfirmedEventHandler>();
                               // Update email options.
                               options.Email.SendEmailOnUpdate = true;
                               // Update phone number options.
                               options.PhoneNumber.SendOtpOnUpdate = true;
                               // Add custom initial user and enable test data.
                               options.UseInitialData = true;
                           })
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
                               options.ClientErrorMapping[500].Link = "https://httpstatuses.com/500";
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
                               options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                               options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                               options.JsonSerializerOptions.IgnoreNullValues = true;
                           })
                           .AddFluentValidation(options => {
                               options.RegisterValidatorsFromAssemblyContaining<Startup>();
                               options.ConfigureClientsideValidation();
                               options.RunDefaultMvcValidationAfterFluentValidationExecutes = true;
                           })
                           .AddAvatars()
                           .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);
        }
    }
}