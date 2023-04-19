using Microsoft.Net.Http.Headers;
using UAParser;

namespace Microsoft.AspNetCore.Http;

public static class HttpRequestExtensions
{
    public static string GetBrowserName(this HttpRequest request) {
        var userAgent = request.Headers[HeaderNames.UserAgent];
        ClientInfo clientInfo = null;
        if (!string.IsNullOrWhiteSpace(userAgent)) {
            var uaParser = Parser.GetDefault();
            clientInfo = uaParser.Parse(userAgent);
        }
        return clientInfo?.UA?.Family;
    }
}
