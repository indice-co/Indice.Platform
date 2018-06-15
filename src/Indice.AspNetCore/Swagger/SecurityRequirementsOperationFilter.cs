using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.Extensions.Options;
using Indice.Configuration;

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

        public void Apply(Operation operation, OperationFilterContext context) {
            // Policy names map to scopes
            
            var requireScopes = context.ControllerActionDescriptor.GetControllerAndActionAttributes(true)
                .OfType<AuthorizeAttribute>()
                .Select(attr => attr.Policy);
            
            if (requireScopes.Any()) {
                operation.Responses.Add("401", new Response { Description = "Unauthorized" });
                operation.Responses.Add("403", new Response { Description = "Forbidden" });
                var scopes = new[] { _ApiSettings.ResourceName }.Union(_ApiSettings.Scopes.Keys.Select(x => $"{_ApiSettings.ResourceName}:{x}"));
                operation.Security = new List<IDictionary<string, IEnumerable<string>>>();
                operation.Security.Add(new Dictionary<string, IEnumerable<string>>
                {
                    { "oauth2", scopes }
                });
            }
        }
    }
}
