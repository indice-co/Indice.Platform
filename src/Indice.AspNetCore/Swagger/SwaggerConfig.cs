using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Indice.Configuration;
using Indice.Serialization;
using Indice.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.AspNetCore.Swagger
{
    /// <summary>
    /// Swagger configuration extensions the Indice way. Exposes usefull defaults for hosting an API. 
    /// Also leverages appsettings.json configuration through <see cref="GeneralSettings"/> for API setup.
    /// </summary>
    public static class SwaggerConfig
    {
        /// <summary>
        /// Since Swashbackle 4.0 release the support for for parameters of type IFormFile is out-of-the-box. 
        /// That is, the generator will automatically detect these and generate the correct Swagger to describe parameters that are passed in formData.
        /// So this is exported to a seperate operation just in case we still need of it.
        /// </summary>
        /// <param name="options">The options used to generate the swagger.json file.</param>
        public static void AddFormFileSupport(this SwaggerGenOptions options) {
            options.OperationFilter<FileUploadOperationFilter>();
            options.OperationFilter<FileDownloadOperationFilter>();
        }

        /// <summary>
        /// Adds support for Fluent validation.
        /// </summary>
        /// <param name="options">The options used to generate the swagger.json file.</param>
        public static void AddFluentValidationSupport(this SwaggerGenOptions options) => options.SchemaFilter<SchemaFluentValidationFilter>();

        /// <summary>
        /// Simplifies generics and removes 'info' suffix.
        /// </summary>
        /// <param name="options">The options used to generate the swagger.json file.</param>
        public static void AddCustomSchemaIds(this SwaggerGenOptions options) {
            // Simplifies generics:
            options.CustomSchemaIds(type => {
                var typeInfo = type.GetTypeInfo();
                var getKeyValueTypeName = new Func<TypeInfo, string>((tInfo) => {
                    var args = tInfo.GetGenericArguments();
                    var param = args[1];
                    var prefix = string.Empty;
                    var name = "Item";
                    return $"{prefix}{name}Of{param.Name}";
                });
                var getGenericTypeName = new Func<TypeInfo, string>((tInfo) => {
                    var args = tInfo.GetGenericArguments();
                    var param = args[0];
                    var prefix = string.Empty;
                    var name = tInfo.Name.Substring(0, tInfo.Name.IndexOf("`"));
                    return $"{prefix}{name}Of{param.Name}";
                });
                if (typeInfo.IsGenericType) {
                    var args = type.GetGenericArguments();
                    if (args.Length == 2 && type.Name.Contains("KeyValuePair")) {
                        return getKeyValueTypeName(typeInfo);
                    } else if (args.Length == 1 || type.Name.Contains("ResultSet")) {
                        var param = args[0];
                        var prefix = string.Empty;
                        var name = type.Name.Substring(0, type.Name.IndexOf("`"));
                        var paramName = param.Name;
                        if (param.Name.Contains("KeyValuePair")) {
                            paramName = getKeyValueTypeName(param.GetTypeInfo());
                        } else if (param.IsGenericType) {
                            paramName = getGenericTypeName(param.GetTypeInfo());
                        }
                        return $"{prefix}{name}Of{paramName}";
                    }
                } else if (typeof(ProblemDetails).IsAssignableFrom(typeInfo)) {
                    return typeInfo.Name;
                } else if (type.Namespace.Contains("Models")) {
                    var name = type.Name.Replace(type.Namespace, string.Empty);
                    name = name.Replace("Info", string.Empty);
                    return name;
                }
                return type.FullName;
            });
        }

        /// <summary>
        /// Adds polymorphism.
        /// </summary>
        /// <param name="options">The options used to generate the swagger.json file.</param>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public static void AddPolymorphism(this SwaggerGenOptions options, IServiceCollection services) {
            var serviceProvider = services.BuildServiceProvider();
            var jsonOptions = serviceProvider.GetService<IOptions<JsonOptions>>();
            if (jsonOptions == null) {
                return;
            }
            var jsonSerializerOptions = jsonOptions.Value.JsonSerializerOptions;
            var polymorphicConverters = jsonSerializerOptions?.Converters?.Where(x => {
                var converterType = x.GetType();
                if (!(converterType.IsGenericType && converterType.GetGenericTypeDefinition().IsAssignableFrom(typeof(JsonPolymorphicConverterFactory<>)))) {
                    return false;
                }
                return true;
            })
            .Cast<IJsonPolymorphicConverterFactory>();
            if (!polymorphicConverters.Any()) {
                return;
            }
            foreach (var converter in polymorphicConverters) {
                var baseType = converter.BaseType;
                var discriminator = jsonSerializerOptions.PropertyNamingPolicy.ConvertName(converter.TypePropertyName);
                var mapping = JsonPolymorphicUtils.GetTypeMapping(baseType, discriminator);
                options.SchemaFilter<PolymorphicSchemaFilter>(baseType, discriminator, mapping);
                options.OperationFilter<PolymorphicOperationFilter>(new PolymorphicSchemaFilter(baseType, discriminator, mapping));
            }
        }

        /// <summary>
        /// Add a new Swagger document based on a subscope of the existing API.
        /// </summary>
        /// <param name="options">The options used to generate the swagger.json file.</param>
        /// <param name="settings">General settings for an ASP.NET Core application.</param>
        /// <param name="scopeOrGroup">The URL segment that the child scope will live under.</param>
        /// <param name="description">An API description.</param>
        public static OpenApiInfo AddDoc(this SwaggerGenOptions options, GeneralSettings settings, string scopeOrGroup, string description) {
            var apiSettings = settings?.Api ?? new ApiSettings();
            var version = $"v{apiSettings.DefaultVersion}";
            var license = apiSettings.License == null ? null : new OpenApiLicense { Name = apiSettings.License.Name, Url = new Uri(apiSettings.License.Url) };
            var contact = apiSettings.Contact == null ? null : new OpenApiContact { Name = apiSettings.Contact.Name, Url = new Uri(apiSettings.Contact.Url), Email = apiSettings.Contact.Email };
            var scope = apiSettings.GetScope(scopeOrGroup)
                ?? apiSettings.GetScope($"{apiSettings.ResourceName}.{scopeOrGroup}")
                ?? apiSettings.GetScope($"{apiSettings.ResourceName}:{scopeOrGroup}");
            var title = scope?.Description;
            if (scope is null) {
                title = $"{apiSettings.FriendlyName}. {scopeOrGroup}";
            }
            return options.AddDoc(scopeOrGroup, title, description, version, apiSettings.TermsOfServiceUrl, license, contact);
        }

        /// <summary>
        /// Add a new Swagger document based on a subscope of the existing API.
        /// </summary>
        /// <param name="options">The options used to generate the swagger.json file.</param>
        /// <param name="scopeOrGroup">The URL segment that the schild scope will live under</param>
        /// <param name="title">An API title.</param>
        /// <param name="description">An API description.</param>
        /// <param name="version">The API version.</param>
        /// <param name="termsOfService"></param>
        /// <param name="license">An API license URL.</param>
        /// <param name="contact">A contact to communicate for the API.</param>
        public static OpenApiInfo AddDoc(this SwaggerGenOptions options, string scopeOrGroup, string title, string description, string version = "v1", string termsOfService = null, OpenApiLicense license = null, OpenApiContact contact = null) {
            var info = new OpenApiInfo {
                Version = version,
                Title = title,
                Description = description,
                TermsOfService = termsOfService == null ? null : new Uri(termsOfService),
                License = license,
                Contact = contact,
            };
            options.SwaggerDoc(scopeOrGroup, info);
            return info;
        }

        /// <summary>
        /// Adds requirements to the operations protected by the Authorize attribute.
        /// </summary>
        /// <param name="options">The options used to generate the swagger.json file.</param>
        /// <param name="name">The security scheme name to protect.</param>
        /// <param name="settings">General settings for an ASP.NET Core application.</param>
        /// <param name="clearOther">Decides whether to clear existing security requirements.</param>
        public static SwaggerGenOptions AddSecurityRequirements(this SwaggerGenOptions options, string name, GeneralSettings settings, bool clearOther = false) {
            if (clearOther) {
                var filters = options.OperationFilterDescriptors.Where(x => x.Type == typeof(SecurityRequirementsOperationFilter));
                foreach (var filter in filters) {
                    options.OperationFilterDescriptors.Remove(filter);
                }
            }
            options.OperationFilter<SecurityRequirementsOperationFilter>(name, settings);
            return options;
        }

        /// <summary>
        /// Adds Basic authentication via header as a security scheme.
        /// </summary>
        /// <param name="options">The options used to generate the swagger.json file.</param>
        /// <param name="settings">General settings for an ASP.NET Core application.</param>
        /// <param name="name">A unique name for the scheme.</param>
        public static SwaggerGenOptions AddBasicAuthentication(this SwaggerGenOptions options, GeneralSettings settings, string name = "basic_authentication") {
            options.AddSecurityDefinition(name, new OpenApiSecurityScheme {
                Type = SecuritySchemeType.Http,
                Scheme = "basic",
                Description = "Input your username and password to access this API",
                Name = "Authorization",
                In = ParameterLocation.Header
            });
            options.AddSecurityRequirements(name, settings);
            return options;
        }

        /// <summary>
        /// Adds the ability to directly put your JWT for authentication.
        /// </summary>
        /// <param name="options">The options used to generate the swagger.json file.</param>
        /// <param name="settings">General settings for an ASP.NET Core application.</param>
        /// <param name="name">A unique name for the scheme.</param>
        public static SwaggerGenOptions AddJwt(this SwaggerGenOptions options, GeneralSettings settings, string name = "jwt") {
            options.AddSecurityDefinition(name, new OpenApiSecurityScheme() {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                Description = "Input your JWT access token",
                Name = "Authorization",
                In = ParameterLocation.Header
            });
            options.AddSecurityRequirements("JWT", settings);
            return options;
        }

        /// <summary>
        /// Adds OpenId Connect security scheme.
        /// </summary>
        /// <param name="options">The options used to generate the swagger.json file.</param>
        /// <param name="settings">General settings for an ASP.NET Core application.</param>
        /// <param name="name">A unique name for the scheme.</param>
        public static SwaggerGenOptions AddOpenIdConnect(this SwaggerGenOptions options, GeneralSettings settings, string name = "openid") {
            // https://swagger.io/docs/specification/authentication/
            options.AddSecurityDefinition(name, new OpenApiSecurityScheme {
                Type = SecuritySchemeType.OpenIdConnect,
                Description = "Identity Server Openid connect",
                OpenIdConnectUrl = new Uri(settings?.Authority + "/.well-known/openid-configuration")
            });
            options.AddSecurityRequirements(name, settings);
            return options;
        }

        /// <summary>
        /// Adds client credentials security scheme.
        /// </summary>
        /// <param name="options">The options used to generate the swagger.json file.</param>
        /// <param name="settings">General settings for an ASP.NET Core application.</param>
        /// <param name="name">A unique name for the scheme.</param>
        public static SwaggerGenOptions AddClientCredentials(this SwaggerGenOptions options, GeneralSettings settings, string name = "oauth2") {
            // https://swagger.io/docs/specification/authentication/
            var clientCredentialsFlow = new OpenApiOAuthFlow {
                TokenUrl = new Uri(settings?.Authority + "/connect/token"),
                RefreshUrl = new Uri(settings?.Authority + "/connect/token"),
                AuthorizationUrl = new Uri(settings?.Authority + "/connect/authorize"),
                Scopes = GetScopes(settings)
            };
            var oauth2 = options.SwaggerGeneratorOptions.SecuritySchemes.SingleOrDefault(x => x.Value.Type == SecuritySchemeType.OAuth2);
            if (oauth2.Value != null) {
                oauth2.Value.Flows.ClientCredentials = clientCredentialsFlow;
                return options;
            }
            options.AddSecurityDefinition(name, new OpenApiSecurityScheme {
                Type = SecuritySchemeType.OAuth2,
                Description = "Identity Server OAuth2",
                Flows = new OpenApiOAuthFlows {
                    ClientCredentials = clientCredentialsFlow
                }
            });
            options.AddSecurityRequirements(name, settings);
            return options;
        }

        /// <summary>
        /// Adds OAuth 2.0 security scheme using the Authorization Code flow.
        /// </summary>
        /// <param name="options">The options used to generate the swagger.json file.</param>
        /// <param name="settings">General settings for an ASP.NET Core application.</param>
        /// <param name="name">A unique name for the scheme.</param>
        public static SwaggerGenOptions AddOAuth2AuthorizationCodeFlow(this SwaggerGenOptions options, GeneralSettings settings, string name = "oauth2") {
            // https://swagger.io/docs/specification/authentication/
            var authorizationCodeFlow = new OpenApiOAuthFlow {
                TokenUrl = new Uri(settings?.Authority + "/connect/token"),
                RefreshUrl = new Uri(settings?.Authority + "/connect/token"),
                AuthorizationUrl = new Uri(settings?.Authority + "/connect/authorize"),
                Scopes = GetScopes(settings)
            };
            var oauth2 = options.SwaggerGeneratorOptions.SecuritySchemes.SingleOrDefault(x => x.Value.Type == SecuritySchemeType.OAuth2);
            if (oauth2.Value != null) {
                oauth2.Value.Flows.AuthorizationCode = authorizationCodeFlow;
                return options;
            }
            options.AddSecurityDefinition(name, new OpenApiSecurityScheme {
                Type = SecuritySchemeType.OAuth2,
                Description = "Identity Server OAuth2",
                Flows = new OpenApiOAuthFlows {
                    AuthorizationCode = authorizationCodeFlow
                }
            });
            options.AddSecurityRequirements(name, settings);
            return options;
        }

        /// <summary>
        /// Adds OAuth 2.0 security scheme using the Implicit flow.
        /// </summary>
        /// <param name="options">The options used to generate the swagger.json file.</param>
        /// <param name="settings">General settings for an ASP.NET Core application.</param>
        /// <param name="name">A unique name for the scheme.</param>
        public static SwaggerGenOptions AddOAuth2ImplicitFlow(this SwaggerGenOptions options, GeneralSettings settings, string name = "oauth2") {
            // https://swagger.io/docs/specification/authentication/
            var implicitFlow = new OpenApiOAuthFlow {
                TokenUrl = new Uri(settings?.Authority + "/connect/token"),
                RefreshUrl = new Uri(settings?.Authority + "/connect/token"),
                AuthorizationUrl = new Uri(settings?.Authority + "/connect/authorize"),
                Scopes = GetScopes(settings)
            };
            var oauth2 = options.SwaggerGeneratorOptions.SecuritySchemes.SingleOrDefault(x => x.Value.Type == SecuritySchemeType.OAuth2);
            if (oauth2.Value != null) {
                oauth2.Value.Flows.Implicit = implicitFlow;
                return options;
            }
            options.AddSecurityDefinition(name, new OpenApiSecurityScheme {
                Type = SecuritySchemeType.OAuth2,
                Description = "Identity Server OAuth2",
                Flows = new OpenApiOAuthFlows {
                    Implicit = implicitFlow
                }
            });
            options.AddSecurityRequirements(name, settings);
            return options;
        }

        /// <summary>
        /// A set of default settings for exposing an API.
        /// </summary>
        /// <param name="options">The options used to generate the swagger.json file.</param>
        /// <param name="settings">General settings for an ASP.NET Core application.</param>
        public static void IndiceDefaults(this SwaggerGenOptions options, GeneralSettings settings) {
            var apiSettings = settings?.Api ?? new ApiSettings();
            var version = $"v{apiSettings.DefaultVersion}";
            options.SwaggerDoc(apiSettings.ResourceName, new OpenApiInfo {
                Version = version,
                Title = apiSettings.FriendlyName,
                TermsOfService = apiSettings.TermsOfServiceUrl == null ? null : new Uri(apiSettings.TermsOfServiceUrl),
                License = apiSettings.License == null ? null : new OpenApiLicense {
                    Name = apiSettings.License.Name,
                    Url = new Uri(apiSettings.License.Url)
                }
            });
            var xmlFile = $"{Assembly.GetEntryAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath)) {
                options.IncludeXmlComments(xmlPath);
            }
            options.OrderActionsBy(x => x.RelativePath);
            options.MapType<Stream>(() => new OpenApiSchema {
                Type = "string",
                Format = "binary"
            });
            options.MapType<FilterClause>(() => new OpenApiSchema { Type = "string" });
            options.MapType<GeoPoint>(() => new OpenApiSchema { Type = "string" });
            options.MapType<Base64Id>(() => new OpenApiSchema { Type = "string" });
            options.MapType<Base64Host>(() => new OpenApiSchema { Type = "string" });
            options.CustomOperationIds(x => (x.ActionDescriptor as ControllerActionDescriptor)?.ActionName);
        }

        /// <summary>
        /// Includes XML comments from an external assembly. Useful when models are located in more than one assembly.
        /// </summary>
        /// <param name="options">The options to configure.</param>
        /// <param name="assembly">The assembly to scan for XML comments.</param>
        public static void IncludeXmlComments(this SwaggerGenOptions options, Assembly assembly) {
            var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
            if (File.Exists(xmlPath)) {
                options.IncludeXmlComments(xmlPath);
            }
        }

        private static Dictionary<string, string> GetScopes(GeneralSettings settings) {
            var apiSettings = settings?.Api ?? new ApiSettings();
            // Define the OAuth2.0 scheme that's in use (i.e. Implicit Flow).
            var scopes = new Dictionary<string, string> {
                { apiSettings.ResourceName, $"Access to {apiSettings.FriendlyName}"},
            };
            foreach (var scope in apiSettings.Scopes) {
                scopes.Add(scope.Name, scope.Description);
            }
            return scopes;
        }
    }
}
