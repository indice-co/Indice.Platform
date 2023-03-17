using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Extensions;

/// <summary>Extension methods on type <see cref="HttpRequest"/>.</summary>
public static class HttpRequestExtensions
{
    /// <summary>Determines if the content type of the current <see cref="HttpRequest"/> is of type 'application/x-www-form-urlencoded'.</summary>
    /// <param name="request">Represents the incoming side of an individual HTTP request.</param>
    public static bool HasApplicationFormContentType(this HttpRequest request) {
        if (request.ContentType is null) {
            return false;
        }
        if (MediaTypeHeaderValue.TryParse(request.ContentType, out var header)) {
            // Content-Type: application/x-www-form-urlencoded; charset=utf-8
            return header.MediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}
