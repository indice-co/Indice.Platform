using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Indice.AspNetCore.Middleware
{
    //https://www.azureblue.io/how-to-log-http-request-body-with-asp-net-core-application-insights/ 

    // https://exceptionnotfound.net/using-middleware-to-log-requests-and-responses-in-asp-net-core/
    // https://gist.github.com/elanderson/c50b2107de8ee2ed856353dfed9168a2
    // https://stackoverflow.com/a/52328142/3563013
    // https://stackoverflow.com/a/43404745/3563013
    // https://gist.github.com/elanderson/c50b2107de8ee2ed856353dfed9168a2#gistcomment-2319007
    /// <summary>
    /// Middleware that handles the logging of requests and responses.
    /// </summary>
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private RequestResponseLoggingOptions _options;

        /// <summary>
        /// Constructs the <see cref="RequestResponseLoggingMiddleware"/>.
        /// </summary>
        /// <param name="next">A function that can process an HTTP request.</param>
        /// <param name="options">Available configuration options</param>
        public RequestResponseLoggingMiddleware(RequestDelegate next, RequestResponseLoggingOptions options) {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options ?? throw new ArgumentNullException(nameof(next));
        }

        /// <summary>
        /// Invoke the middleware.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        public async Task Invoke(HttpContext context, ILogger<RequestProfilerModel> logger) {
            // Write request body to App Insights
            var loggingHandler = _options.LogHandler ?? DefaultLoggingHandler;
            var model = new RequestProfilerModel(context);
            await model.SnapRequestBody();
            if (await model.NextAndSnapResponceBody(_next, _options.ContentTypes)) {
                await loggingHandler(logger, model);
            }
        }

        /// <summary>
        /// Default logging handler implementation. Adds two info log entries to the <see cref="ILogger"/>. One for the request and the other for the response.
        /// </summary>
        /// <param name="logger">Represents a type used to perform logging.</param>
        /// <param name="model">Represents a request and response in order to be logged.</param>
        public static Task DefaultLoggingHandler(ILogger logger, RequestProfilerModel model) {
            if (model.HttpContext.Response.StatusCode >= 500) {
                logger.LogError(LOG_MESSAGE_FROMAT, model.Duration,
                                                    model.HostUri,
                                                    model.RequestTarget,
                                                    model.StatusCode,
                                                    model.RequestBody,
                                                    model.ResponseBody);
            } else if (model.HttpContext.Response.StatusCode >= 400) {
                logger.LogWarning(LOG_MESSAGE_FROMAT, model.Duration,
                                                    model.HostUri,
                                                    model.RequestTarget,
                                                    model.StatusCode,
                                                    model.RequestBody,
                                                    model.ResponseBody);
            } else {
                logger.LogInformation(LOG_MESSAGE_FROMAT, model.Duration,
                                                    model.HostUri,
                                                    model.RequestTarget,
                                                    model.StatusCode,
                                                    model.RequestBody,
                                                    model.ResponseBody);
            }
            return Task.CompletedTask;
        }

        private const string LOG_MESSAGE_FROMAT = @"Http request 
Duration: {Duration}
Host: {Host}
RequestTarget: {RequestTarget}
StatusCode: {StatusCode}
RequestBody: {RequestBody}
ResponseBody: {ResponseBody}";
    }

    /// <summary>
    /// Represents a request and response in order to be logged.
    /// </summary>
    public class RequestProfilerModel
    {
        private const int ReadChunkBufferLength = 4096;
        /// <summary>
        /// Constructs the model by passing the <see cref="Microsoft.AspNetCore.Http.HttpContext"/>.
        /// </summary>
        /// <param name="httpContext">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
        public RequestProfilerModel(HttpContext httpContext) {
            HttpContext = httpContext;
            var dateText = HttpContext.Request.Headers["Date"];
            if (!DateTime.TryParseExact(dateText, "r", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)) {
                date = DateTime.UtcNow;
            }
            RequestTime = date;
        }

        /// <summary>
        /// Request target is the http request method followed by the request path
        /// </summary>
        public string HostUri => $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}".Trim();

        /// <summary>
        /// Request target is the http request method followed by the request path
        /// </summary>
        public string RequestTarget => $"{HttpContext.Request.Method.ToLowerInvariant()} {HttpContext.Request.Path}".Trim();
        /// <summary>
        /// HTTP status code
        /// </summary>
        public int StatusCode => HttpContext.Response.StatusCode;
        /// <summary>
        /// The request time.
        /// </summary>
        public DateTimeOffset RequestTime { get; }
        /// <summary>
        /// the response time.
        /// </summary>
        public DateTimeOffset ResponseTime { get; private set; }
        /// <summary>
        /// The duration.
        /// </summary>
        public TimeSpan Duration => (ResponseTime - RequestTime);
        /// <summary>
        /// The <see cref="Microsoft.AspNetCore.Http.HttpContext"/> when the request happended.
        /// </summary>
        public HttpContext HttpContext { get; }
        /// <summary>
        /// The request message.
        /// </summary>
        public string RequestBody { get; private set; }
        /// <summary>
        /// Τhe response message.
        /// </summary>
        public string ResponseBody { get; private set; }

        /// <summary>
        /// Takes a snapshot of the current request body.
        /// </summary>
        /// <returns></returns>
        internal async Task SnapRequestBody() {
            var method = HttpContext.Request.Method;
            HttpContext.Request.EnableBuffering();
            // Only if we are dealing with POST or PUT, GET and others shouldn't have a body
            if (HttpContext.Request.Body.CanRead && (HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method))) {
                // Leave stream open so next middleware can read it
                using var reader = new StreamReader(
                    HttpContext.Request.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 512, leaveOpen: true);
                RequestBody = await reader.ReadToEndAsync();
                // Reset stream position, so next middleware can read it
                HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Takes a snapshot of the current request body.
        /// </summary>
        /// <returns></returns>
        internal async Task SnapRequestBodyOld() {
            var request = HttpContext.Request;
            request.EnableBuffering();
            using var requestStream = new MemoryStream();
            await request.Body.CopyToAsync(requestStream);
            request.Body.Seek(0, SeekOrigin.Begin);
            RequestBody = ReadStreamInChunks(requestStream);
        }

        /// <summary>
        /// Takes a snapshot of the response body.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="allowedContentTypes"></param>
        /// <returns></returns>
        internal async Task<bool> NextAndSnapResponceBody(RequestDelegate next, List<string> allowedContentTypes = null) {
            var originalBody = HttpContext.Response.Body;
            using var newResponseBody = new MemoryStream();
            HttpContext.Response.Body = newResponseBody;
            await next(HttpContext);
            newResponseBody.Seek(0, SeekOrigin.Begin);
            await newResponseBody.CopyToAsync(originalBody);
            newResponseBody.Seek(0, SeekOrigin.Begin);
            var ok = allowedContentTypes == null || string.IsNullOrEmpty(HttpContext.Response.ContentType) || allowedContentTypes.Contains(HttpContext.Response.ContentType.Split(';')[0], StringComparer.OrdinalIgnoreCase);
            if (ok) {
                ResponseBody = ReadStreamInChunks(newResponseBody);
            }
            ResponseTime = DateTimeOffset.UtcNow;
            return ok;
        }

        private static string ReadStreamInChunks(Stream stream) {
            stream.Seek(0, SeekOrigin.Begin);
            string result;
            using (var textWriter = new StringWriter())
            using (var reader = new StreamReader(stream)) {
                var readChunk = new char[ReadChunkBufferLength];
                int readChunkLength;
                // do while: is useful for the last iteration in case readChunkLength < chunkLength.
                do {
                    readChunkLength = reader.ReadBlock(readChunk, 0, ReadChunkBufferLength);
                    textWriter.Write(readChunk, 0, readChunkLength);
                } while (readChunkLength > 0);
                result = textWriter.ToString();
            }
            return result;
        }
    }
}
