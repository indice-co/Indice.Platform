using System;
using System.IO;
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
        private readonly Action<ILogger<RequestProfilerModel>, RequestProfilerModel> _requestResponseHandler;
        private const int ReadChunkBufferLength = 4096;

        /// <summary>
        /// Constructs the <see cref="RequestResponseLoggingMiddleware"/>.
        /// </summary>
        /// <param name="next">A function that can process an HTTP request.</param>
        /// <param name="requestResponseHandler"></param>
        public RequestResponseLoggingMiddleware(RequestDelegate next, Action<ILogger<RequestProfilerModel>, RequestProfilerModel> requestResponseHandler) {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _requestResponseHandler = requestResponseHandler ?? throw new ArgumentNullException(nameof(requestResponseHandler));
        }

        /// <summary>
        /// Invoke the middleware.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        public async Task Invoke(HttpContext context, ILogger<RequestProfilerModel> logger) {
            var loggingHandler = _requestResponseHandler ?? DefaultLoggingHandler;
            var model = new RequestProfilerModel {
                RequestTime = new DateTimeOffset(),
                Context = context,
                Request = await FormatRequest(context)
            };
            var originalBody = context.Response.Body;
            using var newResponseBody = new MemoryStream();
            context.Response.Body = newResponseBody;
            await _next(context);
            newResponseBody.Seek(0, SeekOrigin.Begin);
            await newResponseBody.CopyToAsync(originalBody);
            newResponseBody.Seek(0, SeekOrigin.Begin);
            model.Response = FormatResponse(context, newResponseBody);
            model.ResponseTime = new DateTimeOffset();
            loggingHandler(logger, model);
        }

        /// <summary>
        /// Default logging handler implementation. Adds two info log entries to the <see cref="ILogger"/>. One for the request and the other for the response.
        /// </summary>
        /// <param name="logger">Represents a type used to perform logging.</param>
        /// <param name="model">Represents a request and response in order to be logged.</param>
        public static void DefaultLoggingHandler(ILogger<RequestProfilerModel> logger, RequestProfilerModel model) {
            if (model.Context.Response.StatusCode >= 500) {
                logger.LogError(model.Request);
                logger.LogError(model.Response);
            } else if (model.Context.Response.StatusCode >= 400) {
                logger.LogWarning(model.Request);
                logger.LogWarning(model.Response);
            } else {
                logger.LogInformation(model.Request);
                logger.LogInformation(model.Response);
            }
        }

        private string FormatResponse(HttpContext context, MemoryStream newResponseBody) {
            var request = context.Request;
            var response = context.Response;
            return $"Http Response Information: {Environment.NewLine}" +
                   $"Scheme: {request.Scheme} {Environment.NewLine}" +
                   $"Host: {request.Host} {Environment.NewLine}" +
                   $"Path: {request.Path} {Environment.NewLine}" +
                   $"QueryString: {request.QueryString} {Environment.NewLine}" +
                   $"StatusCode: {response.StatusCode} {Environment.NewLine}" +
                   $"Response Body: {ReadStreamInChunks(newResponseBody)}";
        }

        private async Task<string> FormatRequest(HttpContext context) {
            var request = context.Request;
            return $"Http Request Information: {Environment.NewLine}" +
                   $"Scheme: {request.Scheme} {Environment.NewLine}" +
                   $"Host: {request.Host} {Environment.NewLine}" +
                   $"Path: {request.Path} {Environment.NewLine}" +
                   $"QueryString: {request.QueryString} {Environment.NewLine}" +
                   $"Request Body: {await GetRequestBody(request)}";
        }

        private async Task<string> GetRequestBody(HttpRequest request) {
            request.EnableBuffering();
            using var requestStream = new MemoryStream();
            await request.Body.CopyToAsync(requestStream);
            request.Body.Seek(0, SeekOrigin.Begin);
            return ReadStreamInChunks(requestStream);
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

    /// <summary>
    /// Represents a request and response in order to be logged.
    /// </summary>
    public class RequestProfilerModel
    {
        /// <summary>
        /// The request time.
        /// </summary>
        public DateTimeOffset RequestTime { get; set; }
        /// <summary>
        /// the response time.
        /// </summary>
        public DateTimeOffset ResponseTime { get; set; }
        /// <summary>
        /// The duration.
        /// </summary>
        public TimeSpan Duration => (ResponseTime - RequestTime);
        /// <summary>
        /// The <see cref="HttpContext"/> when the request happended.
        /// </summary>
        public HttpContext Context { get; set; }
        /// <summary>
        /// The request message.
        /// </summary>
        public string Request { get; set; }
        /// <summary>
        /// Τhe response message.
        /// </summary>
        public string Response { get; set; }
    }
}
