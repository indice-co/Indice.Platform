using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.MultitenantApi.Swagger.Filters
{
    public class TenantIdHeaderParameterOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context) {
            var filterPipeline = context.ApiDescription.ActionDescriptor.FilterDescriptors;
            if (operation.Parameters == null) {
                operation.Parameters = new List<OpenApiParameter>();
            }
            operation.Parameters.Add(new OpenApiParameter {
                Name = "X-Tenant-Id",
                In = ParameterLocation.Header,
                Description = "The id of the tenant",
                Required = false,
                Schema = new OpenApiSchema {
                    Type = "string",
                    Default = new OpenApiString(string.Empty)
                }
            });
        }
    }
}
