using System.IO;
using System.Linq;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.AspNetCore.Swagger
{
    internal class FileOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context) {
            if (context.ApiDescription.SupportedResponseTypes.Any(x => x.Type == typeof(Stream))) {
                foreach (var responseType in operation.Responses.Where(x => x.Value.Schema?.Type == "file")) {
                    operation.Produces = new[] { "application/octet-stream" };
                }
            }
        }
    }
}
