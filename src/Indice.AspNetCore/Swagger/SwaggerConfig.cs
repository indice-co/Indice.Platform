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
    /// Also leverages appsettings.json configuration through <see cref="GeneralSettings"/> for api setup
    /// </summary>
    public static class SwaggerConfig
    {
        /// <summary>
        /// Since swashbackle 4.0 release the support for for parameters of type IFormFile is out-of-the-box. 
        /// That is, the generator will automatically detect these and generate the correct Swagger to describe parameters that are passed in formData.
        /// So this is exported to a seperate operation just in case we still need of it.
        /// </summary>
        /// <param name="options">The options to confugure</param>
        public static void AddFormFileSupport(this SwaggerGenOptions options) {
            options.OperationFilter<FormFileOperationFilter>();
        }

        /// <summary>
        /// Adds support for Fluent validation.
        /// </summary>
        /// <param name="options">The options to confugure</param>
        public static void AddFluentValidationSupport(this SwaggerGenOptions options) {
            options.SchemaFilter<SchemaFluentValidationFilter>();
        }

        /// <summary>
        /// Simplifies generics and removes 'info' suffix.
        /// </summary>
        /// <param name="options">The options to configure.</param>
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
        /// <param name="options">The options to configure.</param>
        /// <param name="assembliesToScan">Assemblies that will be searched for <see cref="JsonNetPolymorphicConverter"/> annotations.</param>
        public static void AddPolymorphismJsonNet(this SwaggerGenOptions options, params Assembly[] assembliesToScan) {
            var attribute = assembliesToScan.SelectMany(x => x.ExportedTypes)
                                            .Select(x => x.GetCustomAttribute<JsonConverterAttribute>(false))
                                            .Where(x => x != null && typeof(JsonNetPolymorphicConverter)
                                            .IsAssignableFrom(x.ConverterType));
            foreach (var item in attribute) {
                var baseType = item.ConverterType.GenericTypeArguments[0];
                var discriminator = item.ConverterParameters?.FirstOrDefault() as string;
                var mapping = JsonNetPolymorphicConverter.GetTypeMapping(baseType, discriminator);
                options.SchemaFilter<PolymorphicSchemaFilter>(baseType, discriminator, mapping);
                options.OperationFilter<PolymorphicOperationFilter>(new PolymorphicSchemaFilter(baseType, discriminator, mapping));
            }
        }

        /// <summary>
        /// Adds polymorphism.
        /// </summary>
        /// <param name="options">The options to configure.</param>
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
                var discriminator = converter.TypePropertyName;
                var mapping = JsonPolymorphicConverter.GetTypeMapping(baseType, discriminator);
                options.SchemaFilter<PolymorphicSchemaFilter>(baseType, discriminator, mapping);
                options.OperationFilter<PolymorphicOperationFilter>(new PolymorphicSchemaFilter(baseType, discriminator, mapping));
            }
        }

        /// <summary>
        /// Add a new swagger document based on a subscope of the existing api.
        /// </summary>
        /// <param name="options">The options to confugure</param>
        /// <param name="settings"></param>
        /// <param name="scopeOrGroup">The url segment that the schild scope will live under</param>
        /// <param name="description"></param>
        public static OpenApiInfo AddDoc(this SwaggerGenOptions options, GeneralSettings settings, string scopeOrGroup, string description) {
            var apiSettings = settings?.Api ?? new ApiSettings();
            var version = $"v{apiSettings.DefaultVersion}";
            var scope = scopeOrGroup;
            var license = apiSettings.License == null ? null : new OpenApiLicense { Name = apiSettings.License.Name, Url = new Uri(apiSettings.License.Url) };
            var contact = apiSettings.Contact == null ? null : new OpenApiContact { Name = apiSettings.Contact.Name, Url = new Uri(apiSettings.Contact.Url), Email = apiSettings.Contact.Email };
            var title = $"{apiSettings.FriendlyName}. {scopeOrGroup}";
            apiSettings.Scopes.TryGetValue(scopeOrGroup, out title);
            return options.AddDoc(scope, title, description, version, apiSettings.TermsOfServiceUrl, license, contact);
        }

        /// <summary>
        /// Add a new swagger document based on a subscope of the existing api.
        /// </summary>
        /// <param name="options">The options to confugure</param>
        /// <param name="scopeOrGroup">The url segment that the schild scope will live under</param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="version"></param>
        /// <param name="termsOfService"></param>
        /// <param name="license"></param>
        /// <param name="contact"></param>
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
        /// Adds requirements to the operations protected by the Authorize attribute
        /// </summary>
        /// <param name="options"></param>
        /// <param name="name">The security scheme name to protect.</param>
        /// <param name="settings"></param>
        /// <param name="clearOther"></param>
        /// <returns></returns>
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
        /// adds Basic authentication via header as a security scheme.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="settings"></param>
        /// <param name="name">A unique name for the scheme.</param>
        public static SwaggerGenOptions AddBasicAuthentication(this SwaggerGenOptions options, GeneralSettings settings, string name = "basicAuth") {
            //options.SwaggerGeneratorOptions.SecuritySchemes.Clear();
            options.AddSecurityDefinition(name, new OpenApiSecurityScheme() {
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
        /// adds OpenId connect security scheme.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="settings"></param>
        /// <param name="name">A unique name for the scheme.</param>
        public static SwaggerGenOptions AddOpenIdConnect(this SwaggerGenOptions options, GeneralSettings settings, string name = "openId") {
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
        /// adds OpenId connect security scheme.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="settings"></param>
        /// <param name="name">A unique name for the scheme.</param>
        public static SwaggerGenOptions AddOAuth2(this SwaggerGenOptions options, GeneralSettings settings, string name = "oauth2") {
            var apiSettings = settings?.Api ?? new ApiSettings();
            // Define the OAuth2.0 scheme that's in use (i.e. Implicit Flow).
            var scopes = new Dictionary<string, string> {
                { apiSettings.ResourceName, $"Access to {apiSettings.FriendlyName}"},
            };
            foreach (var scope in apiSettings.Scopes) {
                scopes.Add($"{apiSettings.ResourceName}:{scope.Key}", scope.Value);
            }
            // https://swagger.io/docs/specification/authentication/
            options.AddSecurityDefinition(name, new OpenApiSecurityScheme {
                Type = SecuritySchemeType.OAuth2,
                Description = "Identity Server oAuth2",
                Flows = new OpenApiOAuthFlows {
                    Implicit = new OpenApiOAuthFlow {
                        TokenUrl = new Uri(settings?.Authority + "/connect/token"),
                        RefreshUrl = new Uri(settings?.Authority + "/connect/token"),
                        AuthorizationUrl = new Uri(settings?.Authority + "/connect/authorize"),
                        Scopes = scopes
                    },
                    ClientCredentials = new OpenApiOAuthFlow {
                        TokenUrl = new Uri(settings?.Authority + "/connect/token"),
                        RefreshUrl = new Uri(settings?.Authority + "/connect/token"),
                        AuthorizationUrl = new Uri(settings?.Authority + "/connect/authorize"),
                        Scopes = scopes
                    }
                }
            });
            options.AddSecurityRequirements(name, settings);
            return options;
        }

        /// <summary>
        /// A set of defaults for exposing an API.
        /// </summary>
        /// <param name="options">The options to confugure</param>
        /// <param name="settings"></param>
        public static void IndiceDefaults(this SwaggerGenOptions options, GeneralSettings settings) {
            var apiSettings = settings?.Api ?? new ApiSettings();
            var version = $"v{apiSettings.DefaultVersion}";
            options.SwaggerDoc(apiSettings.ResourceName, new OpenApiInfo {
                Version = version,
                Title = apiSettings.FriendlyName,
                TermsOfService = apiSettings.TermsOfServiceUrl == null ? null : new Uri(apiSettings.TermsOfServiceUrl),
                License = apiSettings.License == null ? null : new OpenApiLicense { Name = apiSettings.License.Name, Url = new Uri(apiSettings.License.Url) }
            });
            var xmlFile = $"{Assembly.GetEntryAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath)) {
                options.IncludeXmlComments(xmlPath);
            }
            //options.UseReferencedDefinitionsForEnums();
            options.OrderActionsBy(d => d.RelativePath);
            options.MapType<Stream>(() => new OpenApiSchema {
                Type = "string",
                Format = "binary"
            });
            options.MapType<FilterClause>(() => new OpenApiSchema {
                Type = "string"
            });
            options.MapType<GeoPoint>(() => new OpenApiSchema {
                Type = "string"
            });
            options.CustomOperationIds(x => (x.ActionDescriptor as ControllerActionDescriptor)?.ActionName);
        }

        /// <summary>
        /// Include xml comments from an external assembly. Useful when models are located in more than one assembly.
        /// </summary>
        /// <param name="options">The options to configure.</param>
        /// <param name="assembly">The assembly to scan for XML comments.</param>
        public static void IncludeXmlComments(this SwaggerGenOptions options, Assembly assembly) {
            var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
            if (File.Exists(xmlPath)) {
                options.IncludeXmlComments(xmlPath);
            }
        }
    }
}
