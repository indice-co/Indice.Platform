using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Middleware;

/// <summary>Logging options for <see cref="RequestResponseLoggingMiddleware"/></summary>
public class RequestResponseLoggingOptions
{
    /// <summary>These are the response content types that will be tracked. Others are filtered out. Whern null or empty array uses defaults application/json and text/html</summary>
    public List<string> ContentTypes { get; set; } = new List<string> { "application/json", "text/html" };

    /// <summary>Optionally pass a custon hander that will be used instead of the default internal one.</summary>
    public Func<ILogger, RequestProfilerModel, Task> LogHandler { get; set; }
}
