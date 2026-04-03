#if NET9_0
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Provides extension methods to add example transformers to OpenAPI options.</summary>
public static class ExamplesTransformer
{
    /// <summary>
    /// Adds an operation transformer to the specified OpenApiOptions that injects example request bodies for endpoints
    /// annotated with OperationExampleMetadata.
    /// </summary>
    /// <remarks>This method enables automatic population of example request bodies in OpenAPI documentation
    /// for endpoints that include OperationExampleMetadata. The transformer will set the example for the corresponding
    /// content type if such metadata is present.</remarks>
    /// <param name="options">The OpenApiOptions instance to which the example transformer will be added.</param>
    /// <returns>The same OpenApiOptions instance, enabling method chaining.</returns>
    public static OpenApiOptions AddExamplesTransformer(this OpenApiOptions options) {
        options.AddOperationTransformer((operation, context, cancellationToken) => {
            
            if (context.Description.ActionDescriptor.EndpointMetadata.OfType<EndpointBodyExampleMetadata>().Any()) {
                var exampleMetadata = context.Description.ActionDescriptor.EndpointMetadata.OfType<EndpointBodyExampleMetadata>().First();
                var endpointName = context.Description.ActionDescriptor.EndpointMetadata.OfType<EndpointNameMetadata>().FirstOrDefault()?.EndpointName;
                if (operation.RequestBody is not null && 
                    operation.RequestBody.Content.ContainsKey(exampleMetadata.ContentType)) {
                    operation.RequestBody.Content[exampleMetadata.ContentType].Example = exampleMetadata.Value;
                }
            }
            return Task.CompletedTask;
        });
        return options;
    }
}

#endif