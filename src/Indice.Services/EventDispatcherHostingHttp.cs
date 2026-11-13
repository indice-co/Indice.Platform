using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Indice.Extensions;
using Indice.Serialization;
using Microsoft.Extensions.Logging;

namespace Indice.Services;

/// <summary>
/// An implementation of <see cref="IEventDispatcher"/> that publishes the events to the queues of a remote workers API (<c>Indice.Features.Workers.Server</c>) over HTTP.
/// It is the remote counterpart of <c>EventDispatcherHosting</c>: the message lands in the same relational queue and is consumed by the workers that are hosted along with the API.
/// </summary>
/// <remarks>
/// The event is serialized as is, since the workers deserialize the work item type directly. So <c>wrap</c>, <c>prependEnvironmentInQueueName</c>, <c>actingPrincipal</c> and <c>sessionId</c> are ignored.
/// The machine-to-machine client must own the <c>workers:producer</c> scope.
/// </remarks>
public class EventDispatcherHostingHttp : IEventDispatcher
{
    /// <summary>The name of the <see cref="HttpClient"/> used by the dispatcher. A keyed dispatcher uses <c>{HttpClientName}:{name}</c>.</summary>
    public const string HttpClientName = "Indice.EventDispatcherHostingHttp";
    private readonly HttpClient _httpClient;
    private readonly EventDispatcherHostingHttpOptions _options;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly ILogger<EventDispatcherHostingHttp>? _logger;

    /// <summary>Creates a new instance of <see cref="EventDispatcherHostingHttp"/>.</summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> that calls the workers API. It is expected to attach the access token.</param>
    /// <param name="options">The dispatcher options.</param>
    /// <param name="logger">Logger instance for logging warnings and errors.</param>
    public EventDispatcherHostingHttp(HttpClient httpClient, EventDispatcherHostingHttpOptions options, ILogger<EventDispatcherHostingHttp>? logger = null) {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _jsonSerializerOptions = options.JsonSerializerOptions ?? JsonSerializerOptionDefaults.GetDefaultSettings(JavaScriptEncoder.UnsafeRelaxedJsonEscaping);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task RaiseEventAsync<TEvent>(TEvent payload, ClaimsPrincipal? actingPrincipal = null, TimeSpan? visibilityTimeout = null, bool wrap = true, string? queueName = null, bool prependEnvironmentInQueueName = true, string? sessionId = null) where TEvent : class {
        if (!_options.Enabled) {
            return;
        }
        if (!string.IsNullOrWhiteSpace(sessionId)) {
            _logger?.LogWarning("SessionId is not supported by the workers queues. The SessionId '{SessionId}' will be ignored.", sessionId);
        }
        if (string.IsNullOrWhiteSpace(queueName)) {
            queueName = typeof(TEvent).Name.ToKebabCase();
        }
        var request = new EnqueueRequest(
            JsonSerializer.Serialize(payload, _jsonSerializerOptions),
            visibilityTimeout > TimeSpan.Zero ? DateTime.UtcNow.Add(visibilityTimeout.Value) : null
        );
        using var response = await _httpClient.PostAsJsonAsync(GetMessagesUri(queueName), request, RequestJsonOptions);
        if (!response.IsSuccessStatusCode) {
            var body = await response.Content.ReadAsStringAsync();
            _logger?.LogError("Failed to publish event '{EventType}' to queue '{QueueName}'. The workers API responded with {StatusCode}: {Body}", typeof(TEvent).Name, queueName, (int)response.StatusCode, body);
            throw new HttpRequestException($"Failed to publish event '{typeof(TEvent).Name}' to queue '{queueName}'. The workers API responded with {(int)response.StatusCode} ({response.StatusCode}).", null, response.StatusCode);
        }
    }

    private Uri GetMessagesUri(string queueName) {
        var baseAddress = _options.BaseAddress ?? _httpClient.BaseAddress ?? throw new InvalidOperationException($"The {nameof(EventDispatcherHostingHttpOptions.BaseAddress)} of the workers API is not configured.");
        var root = baseAddress.AbsoluteUri.EndsWith('/') ? baseAddress : new Uri(baseAddress.AbsoluteUri + "/");
        var prefix = _options.PathPrefix?.Trim('/');
        var path = string.IsNullOrEmpty(prefix) ? string.Empty : prefix + "/";
        return new Uri(root, $"{path}queues/{Uri.EscapeDataString(queueName)}/messages");
    }

    private static readonly JsonSerializerOptions RequestJsonOptions = new(JsonSerializerDefaults.Web);

    private record EnqueueRequest(string Payload, DateTime? VisibleAt);
}

/// <summary>Options for configuring <see cref="EventDispatcherHostingHttp"/>.</summary>
public class EventDispatcherHostingHttpOptions
{
    /// <summary>The base address of the workers API, for example <c>https://workers.indice.gr/api</c>. Defaults to the <c>General:Endpoints:Workers</c> setting.</summary>
    public Uri? BaseAddress { get; set; }
    /// <summary>The path of the workers endpoints, relative to <see cref="BaseAddress"/>. Defaults to <c>workers</c>, so messages are posted to <c>{BaseAddress}/workers/queues/{queueName}/messages</c>.</summary>
    public string? PathPrefix { get; set; } = "workers";
    /// <summary>Provides a way to enable/disable event dispatching at will. Defaults to true.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>The authority (identity server) that issues the access token. Defaults to the <c>General:Authority</c> setting (the internal one if present).</summary>
    public string? Authority { get; set; }
    /// <summary>The token endpoint. Defaults to <c>{Authority}/connect/token</c>.</summary>
    public string? TokenEndpoint { get; set; }
    /// <summary>The id of the machine-to-machine client (client credentials). When empty, no access token is attached. Defaults to the <c>ClientId</c> secret of <c>General:Api:Secrets</c>.</summary>
    public string? ClientId { get; set; }
    /// <summary>The secret of the machine-to-machine client. Defaults to the <c>ClientSecret</c> secret of <c>General:Api:Secrets</c>.</summary>
    public string? ClientSecret { get; set; }
    /// <summary>The scope requested for the access token. Defaults to <c>workers:producer</c>.</summary>
    public string? Scope { get; set; } = "workers:producer";
    /// <summary>The options used to serialize the event payload. Should match the options of the workers. Defaults to the <see cref="JsonSerializerOptionDefaults"/> with relaxed escaping, as the workers do.</summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    internal string? GetTokenEndpoint() =>
        !string.IsNullOrWhiteSpace(TokenEndpoint) ? TokenEndpoint :
        !string.IsNullOrWhiteSpace(Authority) ? $"{Authority.TrimEnd('/')}/connect/token" : null;
}
