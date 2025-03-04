using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Azure.Messaging.ServiceBus;
using Indice.Extensions;
using Indice.Serialization;
using Indice.Types;
using Microsoft.Extensions.Hosting;

namespace Indice.Services;

/// <inheritdoc/>
public class EventDispatcherAzureServiceBus : IEventDispatcher
{
    /// <summary>The default name of the service bus connection string.</summary>
    public const string CONNECTION_STRING_NAME = "ServiceBusConnection";
    private readonly string _environmentName;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly bool _enabled;
    private readonly bool _useCompression;
    private readonly Func<ClaimsPrincipal> _claimsPrincipalSelector;
    private readonly Func<string?> _tenantIdSelector;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();

    /// <summary>Create a new <see cref="EventDispatcherAzureServiceBus"/> instance.</summary>
    /// <param name="serviceBusClient"></param>
    /// <param name="environmentName">The environment name to use. Defaults to 'Production'.</param>
    /// <param name="enabled">Provides a way to enable/disable event dispatching at will. Defaults to true.</param>
    /// <param name="useCompression">When selected, applies Brotli compression algorithm in the queue message payload. Defaults to false.</param>
    /// <param name="claimsPrincipalSelector">Provides a way to access the current <see cref="ClaimsPrincipal"/> inside a service.</param>
    /// <param name="tenantIdSelector">Provides a way to access the current tenant id if any.</param>
    public EventDispatcherAzureServiceBus(ServiceBusClient serviceBusClient, string environmentName, bool enabled, bool useCompression, Func<ClaimsPrincipal> claimsPrincipalSelector, Func<string> tenantIdSelector) {
        _environmentName = Regex.Replace(environmentName ?? "Development", @"\s+", "-").ToLowerInvariant();
        _serviceBusClient = serviceBusClient;
        _enabled = enabled;
        _useCompression = useCompression;
        _claimsPrincipalSelector = claimsPrincipalSelector ?? throw new ArgumentNullException(nameof(claimsPrincipalSelector));
        _tenantIdSelector = tenantIdSelector ?? new Func<string?>(() => null);
        _jsonSerializerOptions = JsonSerializerOptionDefaults.GetDefaultSettings(JavaScriptEncoder.UnsafeRelaxedJsonEscaping);
    }

    /// <inheritdoc/>
    public async Task RaiseEventAsync<TEvent>(TEvent payload, ClaimsPrincipal? actingPrincipal = null, TimeSpan? visibilityTimeout = null, bool wrap = true, string? queueName = null, bool prependEnvironmentInQueueName = true) where TEvent : class {
        if (!_enabled) {
            return;
        }
        if (string.IsNullOrWhiteSpace(queueName)) {
            queueName = typeof(TEvent).Name.ToKebabCase();
        }
        if (prependEnvironmentInQueueName) {
            queueName = $"{_environmentName}-{queueName}";
        }
        var sender = _senders.GetOrAdd(queueName, _serviceBusClient.CreateSender);
        var user = actingPrincipal ?? _claimsPrincipalSelector?.Invoke();
        byte[] payloadBytes;
        var contentType = MediaTypeNames.Application.Octet;
        // Special cases string, byte[] or stream.
        // if already in binary format mark it so it does not go through compression (twice)
        var isBinary = false;
        switch (payload) {
            case string text: payloadBytes = Encoding.UTF8.GetBytes(text); contentType = $"{MediaTypeNames.Text.Plain}; charset=utf-8"; break;
            case byte[] bytes: payloadBytes = bytes; isBinary = true;  break;
            case ReadOnlyMemory<byte> memory: payloadBytes = memory.ToArray(); isBinary = true; break;
            case Stream stream:
                await using (var memoryStream = new MemoryStream()) {
                    await stream.CopyToAsync(memoryStream);
                    payloadBytes = memoryStream.ToArray();
                }
                isBinary = true;
                break;
            default:
                // Create a message and add it to the queue.
                var jsonPayload = wrap
                    ? JsonSerializer.Serialize(Envelope.Create(user!, payload, _tenantIdSelector()), _jsonSerializerOptions)
                    : JsonSerializer.Serialize(payload, _jsonSerializerOptions);
                payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);
                contentType = $"{MediaTypeNames.Application.Json}; charset=utf-8";
                break;
        }
        var maxTimeSpan = TimeSpan.FromDays(5);
        visibilityTimeout = visibilityTimeout.HasValue && visibilityTimeout.Value > maxTimeSpan ? maxTimeSpan : visibilityTimeout;

        var message = (_useCompression && !isBinary) ? new ServiceBusMessage(new BinaryData(await CompressionUtils.Compress(payloadBytes))) 
                                                     : new ServiceBusMessage(new BinaryData(payloadBytes));
        message.ScheduledEnqueueTime = DateTimeOffset.UtcNow.Add(visibilityTimeout ?? TimeSpan.Zero);
        message.ContentType = contentType;
        await sender.SendMessageAsync(message);
    }
}


/// <summary>Options for configuring <see cref="EventDispatcherAzureServiceBus"/>.</summary>
public class EventDispatcherAzureServiceBusOptions
{
    /// <summary>The connection string to the Azure Storage account. By default it searches for <see cref="EventDispatcherAzureServiceBus.CONNECTION_STRING_NAME"/> application setting inside ConnectionStrings section.</summary>
    public string? ConnectionString { get; set; }
    /// <summary>The environment name to use. Defaults to <see cref="IHostEnvironment.EnvironmentName"/>.</summary>
    public string EnvironmentName { get; set; } = "Production";
    /// <summary>Provides a way to enable/disable event dispatching at will. Defaults to true.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>A function that retrieves the current thread user from the current operation context.</summary>
    public Func<ClaimsPrincipal>? ClaimsPrincipalSelector { get; set; }
    /// <summary>A function that retrieves the current tenant id by any means possible. This is optional.</summary>
    public Func<string>? TenantIdSelector { get; set; }
    /// <summary>When selected, applies Brotli compression algorithm in the queue message payload. Defaults to false.</summary>
    public bool UseCompression { get; set; }
}