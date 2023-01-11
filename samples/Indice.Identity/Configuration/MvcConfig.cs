using System.Text.Json;
using System.Text.Json.Serialization;
using Indice.AspNetCore.Identity.Api.Events;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.AspNetCore.Identity.Events;
using Indice.Identity.Services;
using Indice.Security;
using Indice.Serialization;
using Indice.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;

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
            services.AddPlatformEventHandler<ClientCreatedEvent, ClientCreatedEventHandler>();
            services.AddPlatformEventHandler<EmailConfirmedEvent, UserEmailConfirmedEventHandler>();
            var mvcBuilder = services.AddControllersWithViews()
                                     .AddRazorRuntimeCompilation()
                                     .AddTotp()
                                     .AddDevices(options => {
                                         options.UsePushNotificationsServiceAzure();
                                         options.DefaultTotpDeliveryChannel = TotpDeliveryChannel.Viber;
                                     })
                                     .AddPushNotifications()
                                     .AddIdentityServerApiEndpoints(options => {
                                         options.AddDbContext(context => context.ConfigureDbContext = builder => builder.UseSqlServer(configuration.GetConnectionString("IdentityDb")));
                                         options.CanRaiseEvents = true;
                                         options.DisableCache = true;
                                         options.Email.SendEmailOnUpdate = true;
                                         options.Email.UpdateEmailTemplate = "Email";
                                         options.Email.ForgotPasswordTemplate = "Email";
                                         options.PhoneNumber.SendOtpOnUpdate = true;
                                         options.SeedDummyUsers = false;
                                         options.InitialUsers = GetInitialUsers();
                                         options.CustomClaims = GetCustomClaimTypes();
                                     })
                                     .ConfigureApiBehaviorOptions(options => {
                                         options.ClientErrorMapping[400].Link = "https://httpstatuses.com/400";
                                         options.ClientErrorMapping[401].Link = "https://httpstatuses.com/401";
                                         options.ClientErrorMapping[403].Link = "https://httpstatuses.com/403";
                                         options.ClientErrorMapping[404].Link = "https://httpstatuses.com/404";
                                         options.ClientErrorMapping[404].Link = "https://httpstatuses.com/405";
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
                                         options.JsonSerializerOptions.Converters.Add(new JsonAnyStringConverter());
                                         options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                                     })
                                     .AddAvatars(options => {
                                         options.TileSizes = new[] { 129 };
                                     })
                                     .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix, options => {
                                         options.ResourcesPath = "Resources";
                                     });
            services.AddClientAwareViewLocationExpander();
            return mvcBuilder;
        }

        private static List<User> GetInitialUsers() => new() {
            new User {
                Admin = false,
                CreateDate = DateTime.UtcNow,
                Email = "g.manoltzas@indice.gr",
                EmailConfirmed = true,
                PhoneNumber = "69XXXXXXXX",
                PhoneNumberConfirmed = true,
                LockoutEnabled = true,
                NormalizedEmail = "g.manoltzas@indice.gr".ToUpper(),
                Id = Guid.NewGuid().ToString(),
                UserName = "g.manoltzas@indice.gr",
                NormalizedUserName = "g.manoltzas@indice.gr".ToUpper(),
                PasswordExpirationDate = DateTime.UtcNow.AddYears(1),
                PasswordExpirationPolicy = PasswordExpirationPolicy.Annually,
                PasswordExpired = false,
                PasswordHash = "AH6SA/wuxp9YEfLGROaj2CgjhxZhXDkMB1nD8V7lfQAI+WTM4lGMItjLhhV5ASsq+Q=="
            }
        };

        private static List<ClaimType> GetCustomClaimTypes() => new() {
            new ClaimType {
                Id = $"{Guid.NewGuid()}",
                Name = BasicClaimTypes.DeveloperTotp,
                Reserved = false,
                Required = false,
                UserEditable = false,
                ValueType = Indice.AspNetCore.Identity.Data.Models.ValueType.String
            }
        };
    }
}