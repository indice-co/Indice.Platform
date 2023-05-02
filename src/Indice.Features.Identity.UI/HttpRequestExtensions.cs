using Microsoft.Net.Http.Headers;
using UAParser;

namespace Microsoft.AspNetCore.Http;

/// <summary>Extension methods on type <see cref="HttpRequest"/>.</summary>
public static class HttpRequestExtensions
{
    /// <summary>Gets information about the current browser via User-Agent.</summary>
    /// <param name="request">Represents the incoming side of an individual HTTP request.</param>
    /// <returns>The browser name (User Agent Family).</returns>
    /// <remarks>This extension is used solely for informational purposes and logging. It is not considered fool proof.</remarks>
    public static string GetBrowserName(this HttpRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request), "Parameter request cannot be null.");
        }
        var userAgent = request.Headers[HeaderNames.UserAgent];
        ClientInfo? clientInfo = null;
        if (!string.IsNullOrWhiteSpace(userAgent))
        {
            var uaParser = Parser.GetDefault();
            clientInfo = uaParser.Parse(userAgent);
        }
        return clientInfo?.UA?.Family ?? string.Empty;
    }
}
