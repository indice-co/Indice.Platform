using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Middleware
{
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
        private readonly Func<ILogger<RequestProfilerModel>, RequestProfilerModel, Task> _requestResponseHandler;
        private readonly List<string> _contentTypes;

        /// <summary>
        /// Constructs the <see cref="RequestResponseLoggingMiddleware"/>.
        /// </summary>
        /// <param name="next">A function that can process an HTTP request.</param>
        /// <param name="contentTypes">These are the response content types that will be tracked. Others are filtered out. Whern null or empty array uses defaults application/json and text/html</param>
        /// <param name="requestResponseHandler"></param>
        public RequestResponseLoggingMiddleware(RequestDelegate next, string[] contentTypes, Func<ILogger<RequestProfilerModel>, RequestProfilerModel, Task> requestResponseHandler) {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _contentTypes = new List<string>(contentTypes);
            _requestResponseHandler = requestResponseHandler ?? throw new ArgumentNullException(nameof(requestResponseHandler));
        }

        /// <summary>
        /// Invoke the middleware.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        public async Task Invoke(HttpContext context, ILogger<RequestProfilerModel> logger) {
            var loggingHandler = _requestResponseHandler ?? DefaultLoggingHandler;
            var model = new RequestProfilerModel(context);
            await model.SnapRequestBody();
            if (await model.NextAndSnapResponceBody(_next, _contentTypes)) {
                await loggingHandler(logger, model);
            }
        }

        /// <summary>
        /// Default logging handler implementation. Adds two info log entries to the <see cref="ILogger"/>. One for the request and the other for the response.
        /// </summary>
        /// <param name="logger">Represents a type used to perform logging.</param>
        /// <param name="model">Represents a request and response in order to be logged.</param>
        public static Task DefaultLoggingHandler(ILogger<RequestProfilerModel> logger, RequestProfilerModel model) {
            if (model.Context.Response.StatusCode >= 500) {
                logger.LogError(FormatRequest(model));
                logger.LogError(FormatResponse(model));
            } else if (model.Context.Response.StatusCode >= 400) {
                logger.LogWarning(FormatRequest(model));
                logger.LogWarning(FormatResponse(model));
            } else {
                logger.LogInformation(FormatRequest(model));
                logger.LogInformation(FormatResponse(model));
            }
            return Task.CompletedTask;
        }

        private static string FormatResponse(RequestProfilerModel model) {
            var request = model.Context.Request;
            var response = model.Context.Response;
            return $"Http Response Information: {Environment.NewLine}" +
                   $"Scheme: {request.Scheme} {Environment.NewLine}" +
                   $"Host: {request.Host} {Environment.NewLine}" +
                   $"Path: {request.Path} {Environment.NewLine}" +
                   $"QueryString: {request.QueryString} {Environment.NewLine}" +
                   $"StatusCode: {response.StatusCode} {Environment.NewLine}" +
                   $"Response Body: {model.ResponseBody}";
        }

        private static string FormatRequest(RequestProfilerModel model) {
            var request = model.Context.Request;
            return $"Http Request Information: {Environment.NewLine}" +
                   $"Scheme: {request.Scheme} {Environment.NewLine}" +
                   $"Host: {request.Host} {Environment.NewLine}" +
                   $"Path: {request.Path} {Environment.NewLine}" +
                   $"QueryString: {request.QueryString} {Environment.NewLine}" +
                   $"Request Body: {model.RequestBody}";
        }

    }

    /// <summary>
    /// Represents a request and response in order to be logged.
    /// </summary>
    public class RequestProfilerModel
    {
        private const int ReadChunkBufferLength = 4096;
        /// <summary>
        /// Constructs the model by passing the <see cref="HttpContext"/>.
        /// </summary>
        /// <param name="httpContext">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
        public RequestProfilerModel(HttpContext httpContext) {
            Context = httpContext;
            var dateText = Context.Request.Headers["Date"];
            if (!DateTime.TryParseExact((string)dateText, "r", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)) {
                date = DateTime.UtcNow;
            }
            RequestTime = date;
        }

        /// <summary>
        /// Request target is the http request method followed by the request path
        /// </summary>
        public string RequestTarget => $"{Context.Request.Method.ToLowerInvariant()} {Context.Request.Path}".Trim();
        /// <summary>
        /// HTTP status code
        /// </summary>
        public int StatusCode => Context.Response.StatusCode;
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
        /// The <see cref="HttpContext"/> when the request happended.
        /// </summary>
        public HttpContext Context { get; }
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
            var request = Context.Request;
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
            var originalBody = Context.Response.Body;
            using var newResponseBody = new MemoryStream();
            Context.Response.Body = newResponseBody;
            await next(Context);
            newResponseBody.Seek(0, SeekOrigin.Begin);
            await newResponseBody.CopyToAsync(originalBody);
            newResponseBody.Seek(0, SeekOrigin.Begin);
            var ok = allowedContentTypes == null || string.IsNullOrEmpty(Context.Response.ContentType) || allowedContentTypes.Contains(Context.Response.ContentType.Split(';')[0], StringComparer.OrdinalIgnoreCase);
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
