using System.IO;
using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.AspNetCore.Swagger
{
    public class FileOperationFilter : IOperationFilter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="context"></param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context) {
            if (context.ApiDescription.SupportedResponseTypes.Any(x => x.Type == typeof(Stream))) {

                foreach (var responseType in operation.Responses) {
                    int.TryParse(responseType.Key, out var responseCode);
                    if (responseCode >= 200 && responseCode < 300) {
                        if (responseType.Value.Content.ContainsKey("application/octet-stream")) {
                            responseType.Value.Content["application/octet-stream"] = new OpenApiMediaType() {
                                Schema = new OpenApiSchema {
                                    Type = "string",
                                    Format = "binary"
                                }
                            };
                        } else {
                            responseType.Value.Content.Clear();
                            responseType.Value.Content.Add("application/octet-stream", new OpenApiMediaType() {
                                Schema = new OpenApiSchema {
                                    Type = "string",
                                    Format = "binary"
                                }
                            });
                        }
                        continue;
                    } else {
                        responseType.Value.Content.Clear();
                        responseType.Value.Content.Add("application/json", new OpenApiMediaType() {
                            Schema = new OpenApiSchema {
                                Type = "object",
                            }
                        });
                    }
                }
            }
        }
    }
}
