using Indice.Events;
using Indice.Features.ActivityLogs.Models;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core;

/// <summary>
/// Factory interface for creating activity log entries.
/// </summary>
public interface IActivityLogFactory
{
    /// <summary>
    /// Creates an activity log entry from a platform event.
    /// </summary>
    /// <param name="event"></param>
    public ActivityLogEntry? CreateFrom(IPlatformEvent @event);
}

/// <inheritdoc/>
public class ActivityLogFactory : IActivityLogFactory
{
    /// <inheritdoc/>
    public ActivityLogEntry? CreateFrom(IPlatformEvent @event) => @event switch {
        // User events
        UserCreatedEvent e => CreateUserEntry(e.User, nameof(UserCreatedEvent), ActivityLogCategories.User, "User created"),
        UserDeletedEvent e => CreateUserEntry(e.User, nameof(UserDeletedEvent), ActivityLogCategories.User, "User deleted"),
        UserBlockedEvent e => CreateUserEntry(e.User, nameof(UserBlockedEvent), ActivityLogCategories.User, "User blocked"),
        UserUnBlockedEvent e => CreateUserEntry(e.User, nameof(UserUnBlockedEvent), ActivityLogCategories.User, "User unblocked"),
        UserNameChangedEvent e => CreateUserEntry(e.User, nameof(UserNameChangedEvent), ActivityLogCategories.User, $"Username changed from '{e.PreviousValue}'"),
        UserEmailChangedEvent e => CreateUserEntry(e.User, nameof(UserEmailChangedEvent), ActivityLogCategories.User, $"Email changed from '{e.PreviousValue}'"),
        AccountLockedEvent e => CreateUserEntry(e.User, nameof(AccountLockedEvent), ActivityLogCategories.User, "Account locked"),
        // Authentication events
        PasswordChangedEvent e => CreateUserEntry(e.User, nameof(PasswordChangedEvent), ActivityLogCategories.Authentication, "Password changed"),
        EmailConfirmedEvent e => CreateUserEntry(e.User, nameof(EmailConfirmedEvent), ActivityLogCategories.Authentication, "Email confirmed"),
        PhoneNumberConfirmedEvent e => CreateUserEntry(e.User, nameof(PhoneNumberConfirmedEvent), ActivityLogCategories.Authentication, "Phone number confirmed"),
        UserRequestForEmailConfirmationEvent e => CreateUserEntry(e.User, nameof(UserRequestForEmailConfirmationEvent), ActivityLogCategories.Authentication, "Email confirmation requested"),
        // Device events
        DeviceCreatedEvent e => CreateDeviceEntry(e.User, e.Device, nameof(DeviceCreatedEvent), "Device created"),
        DeviceUpdatedEvent e => CreateDeviceEntry(e.User, e.Device, nameof(DeviceUpdatedEvent), "Device updated"),
        DeviceDeletedEvent e => CreateDeviceEntry(e.User, e.Device, nameof(DeviceDeletedEvent), "Device deleted"),
        DeviceTrustRequestedEvent e => CreateDeviceEntry(e.User, e.Device, nameof(DeviceTrustRequestedEvent), "Device trust requested"),
        // Client events
        ClientCreatedEvent e => CreateClientEntry(e.Client, nameof(ClientCreatedEvent), "Client created"),
        ClientUpdatedEvent e => CreateClientEntry(e.Client, nameof(ClientUpdatedEvent), "Client updated"),
        ClientDeletedEvent e => CreateClientEntry(e.Client, nameof(ClientDeletedEvent), "Client deleted"),
        _ => null
    };

    private static ActivityLogEntry CreateUserEntry(UserEventContext user, string eventType, string category, string description) => new() {
        EventType = eventType,
        Category = category,
        ResourceId = user.Id,
        ResourceType = "User",
        Description = description,
        Succeeded = true
    };

    private static ActivityLogEntry CreateDeviceEntry(UserEventContext user, UserDeviceEventContext device, string eventType, string description) => new() {
        EventType = eventType,
        Category = ActivityLogCategories.Device,
        ResourceId = device.DeviceId,
        ResourceType = "Device",
        DeviceId = device.DeviceId,
        Description = $"{description}: {device.Name ?? device.Model ?? device.DeviceId}",
        Succeeded = true
    };

    private static ActivityLogEntry CreateClientEntry(ClientEventContext client, string eventType, string description) => new() {
        EventType = eventType,
        Category = ActivityLogCategories.Client,
        //ApplicationId = client.ClientId,
        //ApplicationName = client.ClientName,
        ResourceId = client.ClientId,
        ResourceType = "Client",
        Description = $"{description}: {client.ClientName ?? client.ClientId}",
        Succeeded = true
    };
}