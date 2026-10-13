using System.Text.Json.Nodes;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Indice.Services;

/// <summary>Represents a webhook adapter that can parse incoming webhook requests and convert them into a list of <see cref="WebHookEvent"/>.</summary>
public interface IWebHookHttpHandler
{
    /// <summary>Parses the incoming webhook request and converts it into a list of <see cref="WebHookEvent"/>.</summary>
    Task<WebHookHttpResult> HandeRequestAsync(IDictionary<string, StringValues> requestHeaders, IEnumerable<KeyValuePair<string, StringValues>> queryString, Stream requestBody, CancellationToken cancellationToken);
}

/// <summary>Represents a webhook adapter that can parse incoming webhook requests and convert them into a list of <see cref="WebHookEvent"/>.</summary>
public interface IWebHookHandler
{
    /// <summary>Parses the incoming webhook request and converts it into a list of <see cref="WebHookEvent"/>.</summary>
    Task HandeAsync(List<WebHookEvent> events, CancellationToken cancellationToken);
}

/// <summary>A no-operation implementation of <see cref="IWebHookHandler"/> that does nothing when handling webhook events.</summary>
public class NoOpWebHookHandler : IWebHookHandler
{
    /// <inheritdoc/>
    public Task HandeAsync(List<WebHookEvent> events, CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>Represents the result of parsing a webhook request.</summary>
public class WebHookHttpResult 
{
    /// <summary>Creates a new instance of <see cref="WebHookHttpResult"/>.</summary>
    public WebHookHttpResult(List<WebHookEvent> events) {
        Events = events;
    }
    /// <summary>Gets the list of parsed webhook events.</summary>
    public List<WebHookEvent> Events { get; }
    /// <summary>Gets the list of errors that occurred during parsing.</summary>
    public List<string> Errors { get; } = new();
    /// <summary>Indicates whether the parsing was successful (no errors occurred).</summary>
    public bool Success => !Errors.Any();
    /// <summary>Gets or sets the HTTP status code to return to the webhook sender.</summary>
    public int Status { get; set; } = 200;
    /// <summary>Creates a new <see cref="WebHookHttpResult"/> instance representing an error with the specified status code and error messages.</summary>
    public static WebHookHttpResult FromError(int statusCode, params string[] errors) {
        var result = new WebHookHttpResult(new List<WebHookEvent>());
        result.Status = statusCode;
        result.Errors.AddRange(errors);
        return result;
    }
    /// <summary>Creates a new <see cref="WebHookHttpResult"/> instance representing a forbidden request with the specified error messages.</summary>
    public static WebHookHttpResult ForbiddenRequest(params string[] errors) => FromError(403, errors);
    /// <summary>Creates a new <see cref="WebHookHttpResult"/> instance representing an unauthorized request with the specified error messages.</summary>
    public static WebHookHttpResult UnauthorizedRequest(params string[] errors) => FromError(401, errors);
    /// <summary>Creates a new <see cref="WebHookHttpResult"/> instance representing a bad request with the specified error messages.</summary>
    public static WebHookHttpResult BadRequest(params string[] errors) => FromError(400, errors);
    /// <summary>Creates a new <see cref="WebHookHttpResult"/> instance representing a successful request with the specified events.</summary>
    public static WebHookHttpResult Ok(List<WebHookEvent> events) => new(events);
}

/// <summary>Represents a webhook event.</summary>
public class WebHookEvent
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

/// <summary>Processes webhook events in the background.</summary>
public sealed class WebhookEventSink : BackgroundService
{
    private readonly ChannelReader<List<WebHookEvent>> _channelReader;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebhookEventSink> _logger;

    /// <summary>Initializes a new instance of the <see cref="WebhookEventSink"/> class.</summary>
    public WebhookEventSink(
        Channel<List<WebHookEvent>> channel,
        IServiceProvider serviceProvider,
        ILogger<WebhookEventSink> logger) {
        _channelReader = channel.Reader;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        await foreach (var batchEvents in _channelReader.ReadAllAsync(stoppingToken)) {
            try {
                using var scope = _serviceProvider.CreateScope();
                var webhookHandler = scope.ServiceProvider.GetRequiredService<IWebHookHandler>();
                await webhookHandler.HandeAsync(batchEvents, stoppingToken);
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to handle webhook events: {Events}", batchEvents);
            }
        }
    }
}