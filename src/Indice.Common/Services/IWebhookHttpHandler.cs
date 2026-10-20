using System.Text.Json.Nodes;
using Microsoft.Extensions.Primitives;

namespace Indice.Services;

/// <summary>Represents a webhook adapter that can parse incoming webhook requests and convert them into a list of <see cref="WebhookEvent"/>.</summary>
public interface IWebhookHttpHandler
{
    /// <summary>Parses the incoming webhook request and converts it into a list of <see cref="WebhookEvent"/>.</summary>
    Task<WebhookHttpResult> ParseAsync(
        IDictionary<string, StringValues> requestHeaders,
        IEnumerable<KeyValuePair<string, StringValues>> requestParameters,
        Stream requestBody,
        CancellationToken cancellationToken);
}

/// <summary>Represents a webhook event.</summary>
public sealed class WebhookEvent
{
    /// <summary>Gets or sets the name of the provider that sent the webhook event.</summary>
    public string Provider { get; set; } = null!;
    /// <summary>Gets or sets the type of the webhook event.</summary>
    public string EventType { get; set; } = null!;
    /// <summary>Gets or sets the unique identifier of the webhook event.</summary>
    public string MessageId { get; set; } = null!;
    /// <summary>Gets or sets the timestamp of the webhook event.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the unique identifier of the event.</summary>
    public string EventId { get; set; } = null!;
    /// <summary>Gets or sets the recipient of the webhook event.</summary>
    public string Recipient { get; set; } = null!;
    /// <summary>Gets or sets the subject of the webhook event.</summary>
    public string? Subject { get; set; }
    /// <summary>Gets or sets the payload of the webhook event.</summary>
    public JsonNode? Payload { get; set; }
}

/// <summary>Represents the result of parsing a webhook request.</summary>
public sealed class WebhookHttpResult
{
    /// <summary>Creates a new instance of <see cref="WebhookHttpResult"/>.</summary>
    public WebhookHttpResult(List<WebhookEvent> events) {
        Events = events;
    }
    /// <summary>Gets the list of parsed webhook events.</summary>
    public List<WebhookEvent> Events { get; }
    /// <summary>Gets the list of errors that occurred during parsing.</summary>
    public List<string> Errors { get; } = new();
    /// <summary>Indicates whether the parsing was successful (no errors occurred).</summary>
    public bool Success => !Errors.Any();
    /// <summary>Gets or sets the HTTP status code to return to the webhook sender.</summary>
    public int Status { get; set; } = 200;
    /// <summary>Creates a new <see cref="WebhookHttpResult"/> instance representing an error with the specified status code and error messages.</summary>
    public static WebhookHttpResult FromError(int statusCode, params string[] errors) {
        var result = new WebhookHttpResult(new List<WebhookEvent>());
        result.Status = statusCode;
        result.Errors.AddRange(errors);
        return result;
    }
    /// <summary>Creates a new <see cref="WebhookHttpResult"/> instance representing a forbidden request with the specified error messages.</summary>
    public static WebhookHttpResult ForbiddenRequest(params string[] errors) => FromError(403, errors);
    /// <summary>Creates a new <see cref="WebhookHttpResult"/> instance representing an unauthorized request with the specified error messages.</summary>
    public static WebhookHttpResult UnauthorizedRequest(params string[] errors) => FromError(401, errors);
    /// <summary>Creates a new <see cref="WebhookHttpResult"/> instance representing a bad request with the specified error messages.</summary>
    public static WebhookHttpResult BadRequest(params string[] errors) => FromError(400, errors);
    /// <summary>Creates a new <see cref="WebhookHttpResult"/> instance representing a successful request with the specified events.</summary>
    public static WebhookHttpResult Ok(List<WebhookEvent> events) => new(events);
}