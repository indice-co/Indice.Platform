using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.Extensions.Options;
using Indice.Configuration;
using Microsoft.OpenApi.Models;

namespace Indice.AspNetCore.Swagger
{
    internal class SecurityRequirementsOperationFilter : IOperationFilter
    {
        private readonly ApiSettings _ApiSettings;

        public SecurityRequirementsOperationFilter(IOptions<GeneralSettings> settingsWrapper) {
            if (settingsWrapper == null)
                throw new System.ArgumentNullException(nameof(settingsWrapper));
            _ApiSettings = settingsWrapper.Value.Api ?? new ApiSettings();
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
                operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
                operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });
                var scopes = new[] { _ApiSettings.ResourceName }.Union(_ApiSettings.Scopes.Keys.Select(x => $"{_ApiSettings.ResourceName}:{x}"));
                var oAuthScheme = new OpenApiSecurityScheme {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
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
