using System.Text;
using Indice.Serialization;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Core.Services;

/// <summary>HTTP client for invoking external workflow messages for cases.</summary>
public class ElsaClient : IElsaClient
{
    private readonly HttpClient _httpClient;
    private readonly CasesClientOptions _casesClientOptions;

    /// <summary>Creates a new <see cref="ElsaClient"/> instance.</summary>
    /// <param name="httpClient">The HTTP client used for requests.</param>
    /// <param name="casesClientOptions">The options used to configure workflow endpoint paths.</param>
    public ElsaClient(HttpClient httpClient,
        IOptions<CasesClientOptions> casesClientOptions) {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _casesClientOptions = casesClientOptions?.Value ?? throw new ArgumentNullException(nameof(casesClientOptions));
    }

    /// <summary>Sends an external message payload to the workflow endpoint for the specified workflow instance.</summary>
    /// <param name="workflowId">The workflow instance identifier.</param>
    /// <param name="request">The request payload to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SendExternalMessage(Guid workflowId, object request, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        await SendExternalMessage(workflowId, endpointName: null, request, cancellationToken);
    }

    /// <summary>Sends an external message payload to a named workflow endpoint for the specified workflow instance.</summary>
    /// <param name="workflowId">The workflow instance identifier.</param>
    /// <param name="endpointName">The target endpoint name.</param>
    /// <param name="request">The request payload to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SendExternalMessage(Guid workflowId, string? endpointName, object request, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        var activitiesBasePath = _casesClientOptions.ServerHttpActivitiesBasePath?.TrimEnd('/') ?? string.Empty;
        var endpointSegment = string.IsNullOrWhiteSpace(endpointName)
            ? string.Empty
            : $"/{Uri.EscapeDataString(endpointName)}";
        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(request, JsonSerializerOptionDefaults.GetDefaultSettings()), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{activitiesBasePath}/api/cases/{workflowId}{endpointSegment}", content, cancellationToken);

        await ThrowOrProceedResponse(response, cancellationToken);
    }

    private async Task ThrowOrProceedResponse(HttpResponseMessage response, CancellationToken cancellationToken) {
        var headers = new Dictionary<string, IEnumerable<string>>();
        foreach (var item_ in response.Headers)
            headers[item_.Key] = item_.Value;
        if (response.Content != null && response.Content.Headers != null) {
            foreach (var item_ in response.Content.Headers)
                headers[item_.Key] = item_.Value;
        }

        var status = (int)response.StatusCode;
        if (status == StatusCodes.Status404NotFound) {
            var objectResponse = await ReadObjectResponseAsync<ProblemDetails>(response, headers, cancellationToken).ConfigureAwait(false);
            if (objectResponse.Object == null) {
                throw new CasesClientException("Response was null which was not expected.", status, objectResponse.Text, headers, null);
            }
            throw new CasesClientException<ProblemDetails>("Not Found", status, objectResponse.Text, headers, objectResponse.Object, null);
        }
        if (status == StatusCodes.Status409Conflict) {
            var objectResponse = await ReadObjectResponseAsync<ValidationProblemDetails>(response, headers, cancellationToken).ConfigureAwait(false);
            if (objectResponse.Object == null) {
                throw new CasesClientException("Response was null which was not expected.", status, objectResponse.Text, headers, innerException: null);
            }
            throw new CasesClientException<ValidationProblemDetails>("Conflict", status, objectResponse.Text, headers, objectResponse.Object,
                innerException: null);
        }
        if (status == StatusCodes.Status401Unauthorized) {
            var objectResponse = await ReadObjectResponseAsync<ProblemDetails>(response, headers, cancellationToken).ConfigureAwait(false);
            if (objectResponse.Object == null) {
                throw new CasesClientException("Response was null which was not expected.", status, objectResponse.Text, headers, null);
            }
            throw new CasesClientException<ProblemDetails>("Unauthorized", status, objectResponse.Text, headers, objectResponse.Object, null);
        }
        if (status == StatusCodes.Status403Forbidden) {
            var objectResponse = await ReadObjectResponseAsync<ProblemDetails>(response, headers, cancellationToken).ConfigureAwait(false);
            if (objectResponse.Object == null) {
                throw new CasesClientException("Response was null which was not expected.", status, objectResponse.Text, headers, null);
            }
            throw new CasesClientException<ProblemDetails>("Forbidden", status, objectResponse.Text, headers, objectResponse.Object, null);
        }
        if (status == StatusCodes.Status400BadRequest) {
            var objectResponse = await ReadObjectResponseAsync<ProblemDetails>(response, headers, cancellationToken).ConfigureAwait(false);
            if (objectResponse.Object == null) {
                throw new CasesClientException("Response was null which was not expected.", status, objectResponse.Text, headers, null);
            }
            throw new CasesClientException<ProblemDetails>("Bad request", status, objectResponse.Text, headers, objectResponse.Object, null);
        }
        if (!response.IsSuccessStatusCode) {
            var responseData = response.Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new CasesClientException("The HTTP status code of the response was not expected (" + status + ").", status, responseData ?? "", headers, null);
        }
    }


    /// <summary>Exception raised by <see cref="ElsaClient"/> containing a strongly typed error payload.</summary>
    /// <typeparam name="TResult">The type of the deserialized error payload.</typeparam>
    public partial class CasesClientException<TResult> : CasesClientException
    {
        /// <summary>Gets the typed error result returned by the server.</summary>
        public TResult Result { get; private set; }

        /// <summary>Creates a new <see cref="CasesClientException{TResult}"/> instance.</summary>
        /// <param name="message">The exception message.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="response">The raw response body.</param>
        /// <param name="headers">The response headers.</param>
        /// <param name="result">The typed error result.</param>
        /// <param name="innerException">The inner exception.</param>
        public CasesClientException(string message, int statusCode, string response, IReadOnlyDictionary<string, IEnumerable<string>> headers, TResult result, Exception? innerException)
            : base(message, statusCode, response, headers, innerException) {
            Result = result;
        }
    }

    /// <summary>Exception raised by <see cref="ElsaClient"/> for non-success HTTP responses.</summary>
    public partial class CasesClientException : Exception
    {
        /// <summary>Gets the HTTP status code of the response.</summary>
        public int StatusCode { get; private set; }

        /// <summary>Gets the raw response body.</summary>
        public string Response { get; private set; }

        /// <summary>Gets the response headers.</summary>
        public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; private set; }

        /// <summary>Creates a new <see cref="CasesClientException"/> instance.</summary>
        /// <param name="message">The exception message.</param>
        /// <param name="statusCode">The HTTP status code.</param>
        /// <param name="response">The raw response body.</param>
        /// <param name="headers">The response headers.</param>
        /// <param name="innerException">The inner exception.</param>
        public CasesClientException(string message, int statusCode, string response, IReadOnlyDictionary<string, IEnumerable<string>> headers, Exception? innerException)
#pragma warning disable S3358 // Ternary operators should not be nested
            : base(message + "\n\nStatus: " + statusCode + "\nResponse: \n" + ((response == null) ? "(null)" : response.Substring(0, response.Length >= 512 ? 512 : response.Length)), innerException) {
            StatusCode = statusCode;
            Response = response ?? string.Empty;
            Headers = headers;
        }
#pragma warning restore S3358 // Ternary operators should not be nested

        /// <summary>Returns a string representation of the exception including the HTTP response details.</summary>
        public override string ToString() {
            return string.Format("HTTP Response: \n\n{0}\n\n{1}", Response, base.ToString());
        }
    }

    /// <summary>Reads and deserializes the response body as a typed object.</summary>
    /// <typeparam name="T">The target response type.</typeparam>
    /// <param name="response">The HTTP response.</param>
    /// <param name="headers">The response headers.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A typed object response wrapper.</returns>
    protected virtual async Task<ObjectResponseResult<T>> ReadObjectResponseAsync<T>(HttpResponseMessage response, IReadOnlyDictionary<string, IEnumerable<string>> headers, CancellationToken cancellationToken) {
        if (response == null || response.Content == null) {
#pragma warning disable CS8604 // Possible null reference argument.
            return new ObjectResponseResult<T>(default, string.Empty);
#pragma warning restore CS8604 // Possible null reference argument.
        }
        try {
            using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var typedBody = await System.Text.Json.JsonSerializer.DeserializeAsync<T>(responseStream, JsonSerializerOptionDefaults.GetDefaultSettings(), cancellationToken).ConfigureAwait(false);
#pragma warning disable CS8604 // Possible null reference argument.
            return new ObjectResponseResult<T>(typedBody, string.Empty);
#pragma warning restore CS8604 // Possible null reference argument.
        } catch (System.Text.Json.JsonException exception) {
            var message = "Could not deserialize the response body stream as " + typeof(T).FullName + ".";
            throw new CasesClientException(message, (int)response.StatusCode, string.Empty, headers, exception);
        }
    }

    /// <summary>Represents a typed object response and its raw response text.</summary>
    /// <typeparam name="T">The response object type.</typeparam>
    protected readonly struct ObjectResponseResult<T>
    {
        /// <summary>Creates a new <see cref="ObjectResponseResult{T}"/> instance.</summary>
        /// <param name="responseObject">The typed response object.</param>
        /// <param name="responseText">The raw response text.</param>
        public ObjectResponseResult(T responseObject, string responseText) {
            this.Object = responseObject;
            this.Text = responseText;
        }

        /// <summary>Gets the typed response object.</summary>
        public T Object { get; }

        /// <summary>Gets the raw response text.</summary>
        public string Text { get; }
    }
}

/// <summary>Options used to configure <see cref="ElsaClient"/> behavior.</summary>
public class CasesClientOptions
{
    /// <summary>Gets or sets the root path where HTTP workflow activities are exposed.</summary>
    public string? ServerHttpActivitiesBasePath { get; set; }
}
