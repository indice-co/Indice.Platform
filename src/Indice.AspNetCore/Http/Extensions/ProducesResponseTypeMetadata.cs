#if NET7_0_OR_GREATER
using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Builder;

// Equivalent to the .Produces call to add metadata to endpoints
public sealed class ProducesResponseTypeMetadata : IProducesResponseTypeMetadata
{
    public ProducesResponseTypeMetadata(Type type, int statusCode, string contentType) {
        Type = type;
        StatusCode = statusCode;
        ContentTypes = new[] { contentType };
    }

    public Type Type { get; }
    public int StatusCode { get; }
    public IEnumerable<string> ContentTypes { get; }
}
#endif