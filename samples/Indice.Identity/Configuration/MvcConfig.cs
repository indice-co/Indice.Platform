using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using Indice.AspNetCore.Identity.Api.Events;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Identity;
using Indice.Identity.Services;
using Indice.Security;
using Indice.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ValueType = Indice.AspNetCore.Identity.Data.Models.ValueType;

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
                           .AddPushNotifications()
                           .AddIdentityServerApiEndpoints(options => {
                               // Configure the DbContext.
                               options.AddDbContext(identityOptions => identityOptions.ConfigureDbContext = builder => builder.UseSqlServer(configuration.GetConnectionString("IdentityDb")));
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
                               options.SeedDummyUsers = true;
                               options.InitialUsers = GetInitialUsers();
                               options.CustomClaims = GetCustomClaimTypes();
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
                               options.JsonSerializerOptions.Converters.Add(new JsonAnyStringConverter());
                               options.JsonSerializerOptions.IgnoreNullValues = true;
                           })
                           .AddFluentValidation(options => {
                               options.RegisterValidatorsFromAssemblyContaining<Startup>();
                               options.ConfigureClientsideValidation();
                           })
                           .AddAvatars()
                           .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);
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
                ValueType = ValueType.String
            }
        };
    }
}