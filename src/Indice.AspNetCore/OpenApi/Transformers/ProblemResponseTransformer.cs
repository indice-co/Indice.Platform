#if NET9_0_OR_GREATER
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OpenApi;

namespace Microsoft.Extensions.DependencyInjection;

internal static class ProblemResponseTransformer
{
    // An extension method to add the document and operation transformers that together will add
    // a 4XX response to every operation in the OpenAPI document.
    public static OpenApiOptions AddProblemResponseTransformer(this OpenApiOptions options) {
        options.AddDocumentTransformer((document, context, cancellationToken) => {
            document.Components ??= new();
            document.Components.Responses ??= new Dictionary<string, OpenApiResponse>();
            document.Components.Responses["Problem"] = new() {
                Description = "A problem occurred",
                Content = new Dictionary<string, OpenApiMediaType>() {
                    ["application/problem+json"] = new() {
                        Schema = new() {
                            Reference = new() {
                                Type = ReferenceType.Schema,
                                Id = "Problem"
                            }
                        }
                    }
                }
            };
            return Task.CompletedTask;
        });
        options.AddOperationTransformer((operation, context, cancellationToken) => {
            operation.Responses ??= new();
            operation.Responses["4XX"] = new() {
                Reference = new() {
                    Type = ReferenceType.Response,
                    Id = "Problem"
                }
            };
            return Task.CompletedTask;
        });
        return options;
    }
}
#endif