using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Indice.AspNetCore.Swagger;

/// <summary>Converts response to string of bytes when an action produces a response type of <see cref="IFormFile"/> <see cref="Stream"/>.</summary>
public class FileDownloadOperationFilter : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context) {
        if (context.ApiDescription.SupportedResponseTypes.Any(x => x.Type == typeof(IFormFile) || x.Type == typeof(Stream))) {
            foreach (var responseType in operation.Responses) {
                int.TryParse(responseType.Key, out var responseCode);
                if (responseCode >= 200 && responseCode < 300) {
                    if (responseType.Value.Content.ContainsKey(MediaTypeNames.Application.Octet)) {
                        responseType.Value.Content[MediaTypeNames.Application.Octet] = new OpenApiMediaType {
                            Schema = new OpenApiSchema {
                                Type = "string",
                                Format = "binary"
                            }
                        };
                    } else {
                        responseType.Value.Content.Clear();
                        responseType.Value.Content.Add(MediaTypeNames.Application.Octet, new OpenApiMediaType {
                            Schema = new OpenApiSchema {
                                Type = "string",
                                Format = "binary"
                            }
                        });
                    }
                    continue;
                } else {
                    responseType.Value.Content.Clear();
                    responseType.Value.Content.Add(MediaTypeNames.Application.Json, new OpenApiMediaType {
                        Schema = new OpenApiSchema {
                            Type = "object",
                            Reference = new OpenApiReference {
                                Type = ReferenceType.Schema,
                                // This requires to use the Name of the type, instead of FullName, when the AddCustomSchemaIds method is used.
                                Id = responseCode == 400 ? typeof(ValidationProblemDetails).Name : typeof(ProblemDetails).Name
                            }
                        }
                    });
                }
            }
        }
    }
}
