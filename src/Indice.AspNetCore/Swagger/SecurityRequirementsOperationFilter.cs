using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.Extensions.Options;
using Indice.Configuration;
using Microsoft.OpenApi.Models;
using System;

namespace Indice.AspNetCore.Swagger
{
    internal class SecurityRequirementsOperationFilter : IOperationFilter
    {
        private readonly ApiSettings _ApiSettings;
        private readonly string _SecuritySchemeName;

        public SecurityRequirementsOperationFilter(string securitySchemeName, GeneralSettings settings) {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            _ApiSettings = settings.Api ?? new ApiSettings();
            _SecuritySchemeName = securitySchemeName;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="context"></param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context) {
            // Policy names map to scopes
            var authAttributes = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .OfType<AuthorizeAttribute>();
            var requireScopes = authAttributes.Select(attr => attr.Policy);

            if (requireScopes.Any()) {
                if (!operation.Responses.ContainsKey("401"))
                    operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized", Content = new Dictionary<string, OpenApiMediaType> { { "application/json", new OpenApiMediaType { Schema = new OpenApiSchema { Type = "object" } } } } });
                if (!operation.Responses.ContainsKey("403"))
                    operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden", Content = new Dictionary<string, OpenApiMediaType> { { "application/json", new OpenApiMediaType { Schema = new OpenApiSchema { Type = "object" } } } } });
                var scopes = new[] { _ApiSettings.ResourceName }.Union(_ApiSettings.Scopes.Keys.Select(x => $"{_ApiSettings.ResourceName}:{x}"));
                var oAuthScheme = new OpenApiSecurityScheme {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = _SecuritySchemeName }
                };
                operation.Security = new List<OpenApiSecurityRequirement> {
                    new OpenApiSecurityRequirement {
                        [ oAuthScheme ] = scopes.ToList()
                    }
                };
            }
        }
    }
}
