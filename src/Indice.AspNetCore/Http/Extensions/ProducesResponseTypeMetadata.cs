#if NET7_0_OR_GREATER
using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Builder;
/// <summary>Equivalent to the .Produces call to add metadata to endpoints</summary>
public sealed class ProducesResponseTypeMetadata : IProducesResponseTypeMetadata
{
    /// <summary>Constructor</summary>
    public ProducesResponseTypeMetadata(Type type, int statusCode, string contentType) {
        Type = type;
        StatusCode = statusCode;
        ContentTypes = new[] { contentType };
    }

    /// <summary>The clr type for the response body</summary>
    public Type Type { get; }
    /// <summary>Http status code</summary>
    public int StatusCode { get; }
    /// <summary>Supported response body content types</summary>
    public IEnumerable<string> ContentTypes { get; }
}
#endif