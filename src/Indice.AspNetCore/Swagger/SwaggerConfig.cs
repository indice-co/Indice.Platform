using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Indice.Configuration;
using Indice.Types;
using Microsoft.AspNetCore.Hosting;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.AspNetCore.Swagger
{
    public static class SwaggerConfig
    {
        public static void IndiceDefaults(this SwaggerGenOptions options, GeneralSettings settings) {
            var apiSettings = settings?.Api ?? new ApiSettings();

            // Define the OAuth2.0 scheme that's in use (i.e. Implicit Flow).
            var scopes = new Dictionary<string, string> {
                { apiSettings.ResourceName, $"Access to {apiSettings.FriendlyName}"},
            };

            foreach (var scope in apiSettings.Scopes) {
                scopes.Add($"{apiSettings.ResourceName}:{scope.Key}", scope.Value);
            }

            options.AddSecurityDefinition("oauth2", new OAuth2Scheme {
                Type = "oauth2",
                Flow = "implicit",
                Description = "Identity Server OAuth2",
                AuthorizationUrl = settings?.Authority + "/connect/authorize",
                TokenUrl = settings?.Authority + "/connect/token",
                Scopes = scopes,
            });

            var version = $"v{apiSettings.DefaultVersion}";

            options.SwaggerDoc(version, new Info {
                Version = version,
                Title = apiSettings.FriendlyName
            });

            var xmlFile = $"{Assembly.GetEntryAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);
            
            options.DescribeAllEnumsAsStrings();
            options.OrderActionsBy(d => d.RelativePath);

            options.MapType<Stream>(() => new Schema {
                Type = "file"
            });
            options.MapType<FilterClause>(() => new Schema {
                Type = "string"
            });

            options.OperationFilter<SecurityRequirementsOperationFilter>(); // Assign scope requirements to operations based on AuthorizeAttribute.
            options.OperationFilter<SimpleOperationIdFilter>();
            options.OperationFilter<FormFileOperationFilter>();
            options.OperationFilter<FileOperationFilter>();
            options.SchemaFilter<SchemaFluentValidationFilter>();

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
        
    }
}
