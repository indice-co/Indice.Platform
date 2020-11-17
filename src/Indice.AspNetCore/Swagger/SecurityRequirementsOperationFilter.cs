using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using Indice.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.AspNetCore.Swagger
{
    internal class SecurityRequirementsOperationFilter : IOperationFilter
    {
        private readonly ApiSettings _apiSettings;
        private readonly string _securitySchemeName;

        public SecurityRequirementsOperationFilter(string securitySchemeName, GeneralSettings settings) {
            if (settings == null) {
                throw new ArgumentNullException(nameof(settings));
            }
            _apiSettings = settings.Api ?? new ApiSettings();
            _securitySchemeName = securitySchemeName;
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context) {
            var authAttributes = context.MethodInfo.DeclaringType.GetCustomAttributes(true).Union(context.MethodInfo.GetCustomAttributes(true)).OfType<AuthorizeAttribute>();
            var requireScopes = authAttributes.Select(x => x.Policy);
            if (requireScopes.Any()) {
                if (!operation.Responses.ContainsKey("401")) {
                    operation.Responses.Add("401", new OpenApiResponse {
                        Description = "Unauthorized",
                        Content = new Dictionary<string, OpenApiMediaType> {
                            { MediaTypeNames.Application.Json, new OpenApiMediaType { Schema = new OpenApiSchema { Type = "object" } } }
                        }
                    });
                }
                if (!operation.Responses.ContainsKey("403")) {
                    operation.Responses.Add("403", new OpenApiResponse {
                        Description = "Forbidden",
                        Content = new Dictionary<string, OpenApiMediaType> {
                            { MediaTypeNames.Application.Json, new OpenApiMediaType { Schema = new OpenApiSchema { Type = "object" } } }
                        }
                    });
                }
                var scopes = new[] { _apiSettings.ResourceName }.Union(_apiSettings.Scopes.Keys.Select(x => $"{_apiSettings.ResourceName}:{x}"));
                var oAuthScheme = new OpenApiSecurityScheme {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = _securitySchemeName }
                };
                operation.Security = new List<OpenApiSecurityRequirement> {
                    new OpenApiSecurityRequirement {
                        [oAuthScheme] = scopes.ToList()
                    }
                };
            }
        }
    }
}
