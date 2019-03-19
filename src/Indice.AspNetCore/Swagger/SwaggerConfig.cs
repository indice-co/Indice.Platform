using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Indice.Configuration;
using Indice.Serialization;
using Indice.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Swagger;
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
        /// So this is exported to a seperate operation just in case we still need of it. [Depricated]
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
        /// Simplifies generics and removes 'info' suffix
        /// </summary>
        /// <param name="options">The options to confugure</param>
        public static void AddCustomSchemaIds(this SwaggerGenOptions options) {
            // Simplifies generics:
            options.CustomSchemaIds(t => {
                var typeInfo = t.GetTypeInfo();

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
                    var args = t.GetGenericArguments();

                    if (args.Length == 2 && t.Name.Contains("KeyValuePair")) {
                        return getKeyValueTypeName(typeInfo);
                    } else if (args.Length == 1 || t.Name.Contains("ResultSet")) {
                        var param = args[0];
                        var prefix = string.Empty;
                        var name = t.Name.Substring(0, t.Name.IndexOf("`"));
                        var paramName = param.Name;

                        if (param.Name.Contains("KeyValuePair")) {
                            paramName = getKeyValueTypeName(param.GetTypeInfo());
                        } else if (param.IsGenericType) {
                            paramName = getGenericTypeName(param.GetTypeInfo());
                        }

                        return $"{prefix}{name}Of{paramName}";
                    }
                } else if (t.Namespace.Contains("Models")) {
                    var name = t.Name.Replace(t.Namespace, string.Empty);
                    name = name.Replace("Info", string.Empty);

                    return name;
                }

                return t.FullName;
            });
        }

        /// <summary>
        /// Adds polymorphism.
        /// </summary>
        /// <param name="options">The options to confugure</param>
        /// <param name="assembliesToScan">Assemblies that will be searched for <see cref="PolymorphicJsonConverter"/> annotations</param>
        public static void AddPolymorphism(this SwaggerGenOptions options, params Assembly[] assembliesToScan) {
            var attribute = assembliesToScan.SelectMany(x => x.ExportedTypes).Select(x => x.GetCustomAttribute<JsonConverterAttribute>(false))
                                                                             .Where(x => x != null && typeof(PolymorphicJsonConverter).IsAssignableFrom(x.ConverterType));
            foreach (var item in attribute) {
                var baseType = item.ConverterType.GenericTypeArguments[0];
                var discriminator = item.ConverterParameters.FirstOrDefault() as string;
                var mapping = PolymorphicJsonConverter.GetTypeMapping(baseType, discriminator);
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
        public static void AddDoc(this SwaggerGenOptions options, GeneralSettings settings, string scopeOrGroup) {

            var apiSettings = settings?.Api ?? new ApiSettings();
            var version = $"v{apiSettings.DefaultVersion}";

            var scope = scopeOrGroup;
            var description = $"{apiSettings.FriendlyName}. {scopeOrGroup}";
            apiSettings.Scopes.TryGetValue(scopeOrGroup, out description);
            options.SwaggerDoc(scope, new OpenApiInfo {
                Version = version,
                Title = description,
                TermsOfService = apiSettings.TermsOfServiceUrl == null ? null : new Uri(apiSettings.TermsOfServiceUrl),
                License = apiSettings.License == null ? null : new OpenApiLicense { Name = apiSettings.License.Name, Url = new Uri(apiSettings.License.Url) }
            });
        }
        
        /// <summary>
        /// A set of defaults for exposing an API
        /// </summary>
        /// <param name="options">The options to confugure</param>
        /// <param name="settings"></param>
        public static void IndiceDefaults(this SwaggerGenOptions options, GeneralSettings settings) {
            var apiSettings = settings?.Api ?? new ApiSettings();

            // Define the OAuth2.0 scheme that's in use (i.e. Implicit Flow).
            var scopes = new Dictionary<string, string> {
                { apiSettings.ResourceName, $"Access to {apiSettings.FriendlyName}"},
            };

            foreach (var scope in apiSettings.Scopes) {
                scopes.Add($"{apiSettings.ResourceName}:{scope.Key}", scope.Value);
            }
            //https://swagger.io/docs/specification/authentication/
            options.AddSecurityDefinition("openId", new OpenApiSecurityScheme {
                Type = SecuritySchemeType.OpenIdConnect,
                Description = "Identity Server Openid connect",
                OpenIdConnectUrl = new Uri(settings?.Authority + "/.well-known/openid-configuration")
            });
            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme {
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


            var version = $"v{apiSettings.DefaultVersion}";

            options.SwaggerDoc(apiSettings.ResourceName, new OpenApiInfo {
                Version = version,
                Title = apiSettings.FriendlyName,
                TermsOfService = apiSettings.TermsOfServiceUrl == null ? null : new Uri(apiSettings.TermsOfServiceUrl),
                License = apiSettings.License == null ? null : new OpenApiLicense { Name = apiSettings.License.Name, Url = new Uri(apiSettings.License.Url) }
            });

            var xmlFile = $"{Assembly.GetEntryAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);

            options.UseReferencedDefinitionsForEnums();

            options.OrderActionsBy(d => d.RelativePath);

            options.MapType<Stream>(() => new OpenApiSchema {
                Type = "string",
                Format = "binary"
            });
            options.MapType<FilterClause>(() => new OpenApiSchema {
                Type = "string"
            });

            options.OperationFilter<SecurityRequirementsOperationFilter>(); // Assign scope requirements to operations based on AuthorizeAttribute.            
        }

    }
}
