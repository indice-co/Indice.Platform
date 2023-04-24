using Microsoft.Net.Http.Headers;
using UAParser;

namespace Microsoft.AspNetCore.Http;

/// <summary></summary>
public static class HttpRequestExtensions
{
    /// <summary>Tries to identity the browser name from the current <see cref="HttpRequest"/>.</summary>
    /// <param name="request">Represents the incoming side of an individual HTTP request.</param>
    public static string? GetBrowserName(this HttpRequest request) {
        if (request is null) {
            throw new ArgumentNullException(nameof(request), "Parameter request cannot be null.");
        }
        var userAgent = request.Headers[HeaderNames.UserAgent];
        ClientInfo? clientInfo = null;
        if (!string.IsNullOrWhiteSpace(userAgent)) {
            var uaParser = Parser.GetDefault();
            clientInfo = uaParser.Parse(userAgent);
        }
        return clientInfo?.UA?.Family;
    }
}
