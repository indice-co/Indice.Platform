#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// OpenAPI Document Transformer to convert the document to a canonical form. Orders paths, operations, and tags.
/// </summary>
public class CanonicalDocumentTransformer : IOpenApiDocumentTransformer
{
    private class HttpMethodComparer : IComparer<HttpMethod>
    {
        private static readonly List<HttpMethod> Order =
        [
            HttpMethod.Get,
            HttpMethod.Post,
            HttpMethod.Put,
            HttpMethod.Patch,
            HttpMethod.Delete,
            HttpMethod.Head,
            HttpMethod.Options
        ];

        public int Compare(HttpMethod? x, HttpMethod? y)
        {
            if (x is null && y is null) return 0;
            if (x is null) return -1;
            if (y is null) return 1;
            return Order.IndexOf(x).CompareTo(Order.IndexOf(y));
        }
    }

    /// <inheritdoc/>
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // Sort the paths by key
        var sortedPaths = document.Paths.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value);
        document.Paths.Clear();
        foreach (var path in sortedPaths)
        {
            document.Paths.Add(path.Key, path.Value);
        }

        // Sort the operations of each path by type in this order: GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS
        var comparer = new HttpMethodComparer();
        foreach (var path in document.Paths)
        {
            if (path.Value is OpenApiPathItem pathItem)
            {
                var sortedOperations = pathItem.Operations!.OrderBy(o => o.Key, comparer).ToDictionary(o => o.Key, o => o.Value);
                pathItem.Operations!.Clear();
                foreach (var operation in sortedOperations)
                {
                    pathItem.AddOperation(operation.Key, operation.Value);
                }
            }
        }

        // Sort the elements of the tags field by name
        if (document.Tags != null)
        {
            document.Tags = new SortedSet<OpenApiTag>(document.Tags, Comparer<OpenApiTag>.Create((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal)));
        }

        return Task.CompletedTask;
    }
}
#endif