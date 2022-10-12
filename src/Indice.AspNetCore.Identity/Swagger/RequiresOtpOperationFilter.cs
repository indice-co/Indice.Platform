using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Indice.AspNetCore.Identity.Filters;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.AspNetCore.Swagger
{
    /// <summary></summary>
    public class RequiresOtpOperationFilter : IOperationFilter
    {
        private readonly string _headerName;

        /// <summary></summary>
        /// <param name="headerName"></param>
        public RequiresOtpOperationFilter(string headerName) {
            _headerName = headerName ?? RequiresOtpAttribute.DEFAULT_HEADER_NAME;
        }

        /// <inheritdoc />
        public void Apply(OpenApiOperation operation, OperationFilterContext context) {
            operation.Parameters ??= new List<OpenApiParameter>();
            var canGetMethodInfo = context.ApiDescription.TryGetMethodInfo(out var methodInfo);
            if (!canGetMethodInfo) {
                return;
            }
            var actionAttributes = methodInfo.GetCustomAttributes();
            var hasRequiresOtpAttribute = actionAttributes.SingleOrDefault(attribute => attribute.GetType() == typeof(RequiresOtpAttribute)) is not null;
            if (!hasRequiresOtpAttribute) {
                return;
            }
            operation.Parameters.Add(new OpenApiParameter {
                Name = _headerName,
                In = ParameterLocation.Header,
                Description = "The TOTP code.",
                Required = false,
                Schema = context.SchemaGenerator.GenerateSchema(typeof(string), context.SchemaRepository)
            });
        }
    }
}
