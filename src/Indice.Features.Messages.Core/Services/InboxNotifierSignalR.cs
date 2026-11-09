using System.Text.Json;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Serialization;
using Indice.Services;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.Core.Services;

/// <summary>Options for the real time inbox notifications (SignalR).</summary>
public class MessageInboxNotificationOptions
{
    /// <summary>The default hub name.</summary>
    public const string DefaultHubName = "messaging";
    /// <summary>The default client method name.</summary>
    public const string DefaultMethodName = "inboxMessageCreated";

    /// <summary>
    /// The name of the SignalR hub the notifications are broadcast through. Defaults to <i>messaging</i>.
    /// </summary>
    /// <remarks>The same hub name must be allowed in the <c>SignalRProxy:AllowedHubs</c> of the host that exposes the <c>MapSignalRProxy()</c> endpoints the clients negotiate with.</remarks>
    public string HubName { get; set; } = DefaultHubName;
    /// <summary>The client method that will be invoked on the connected clients. Defaults to <i>inboxMessageCreated</i>.</summary>
    public string MethodName { get; set; } = DefaultMethodName;
    /// <summary>Controls whether global campaigns (broadcast to all users) also trigger a notification to every connected user. Defaults to true.</summary>
    public bool NotifyGlobalMessages { get; set; } = true;
}

/// <summary>An implementation of <see cref="IInboxNotifier"/> that pushes notifications to SignalR clients through <see cref="ISignalRProxyBroadcastService"/>.</summary>
public class InboxNotifierSignalR : IInboxNotifier
{
    private static readonly JsonSerializerOptions _jsonOptions = JsonSerializerOptionDefaults.GetDefaultSettings();
    private readonly ISignalRProxyBroadcastService _broadcastService;
    private readonly MessageInboxNotificationOptions _options;

    /// <summary>Creates a new instance of <see cref="InboxNotifierSignalR"/>.</summary>
    /// <param name="broadcastService">The SignalR broadcast service.</param>
    /// <param name="options">The inbox notification options.</param>
    public InboxNotifierSignalR(ISignalRProxyBroadcastService broadcastService, IOptions<MessageInboxNotificationOptions> options) {
        _broadcastService = broadcastService ?? throw new ArgumentNullException(nameof(broadcastService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public Task NotifyUserAsync(string recipientId, InboxMessageNotification notification, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientId);
        ArgumentNullException.ThrowIfNull(notification);
        return _broadcastService.BroadcastToUserAsync(_options.HubName, recipientId, CreateCommand(notification), cancellationToken);
    }

    /// <inheritdoc />
    public Task NotifyAllAsync(InboxMessageNotification notification, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(notification);
        if (!_options.NotifyGlobalMessages) {
            return Task.CompletedTask;
        }
        return _broadcastService.BroadcastToUsersAsync(_options.HubName, CreateCommand(notification), cancellationToken);
    }

    private SignalRBroadcastCommand CreateCommand(InboxMessageNotification notification) =>
        new(_options.MethodName, JsonSerializer.SerializeToNode(notification, _jsonOptions)!);
}
