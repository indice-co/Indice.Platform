using Indice.Events;
using Indice.Features.ActivityLogs;
using Indice.Features.ActivityLogs.Models;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Server;

/// <inheritdoc/>
public class IdentityEventsActivityLogConverter : IActivityLogFromEventConverter
{
    /// <inheritdoc/>
    public ActivityLogEntry? Convert(IPlatformEvent @event) => @event switch {
        // User events
        UserCreatedEvent e => CreateUserEntry(e.User, nameof(UserCreatedEvent), ActivityLogCategories.User, "User created"),
        UserDeletedEvent e => CreateUserEntry(e.User, nameof(UserDeletedEvent), ActivityLogCategories.User, "User deleted"),
        UserBlockedEvent e => CreateUserEntry(e.User, nameof(UserBlockedEvent), ActivityLogCategories.User, "User blocked"),
        UserUnBlockedEvent e => CreateUserEntry(e.User, nameof(UserUnBlockedEvent), ActivityLogCategories.User, "User unblocked"),
        UserNameChangedEvent e => CreateUserEntry(e.User, nameof(UserNameChangedEvent), ActivityLogCategories.User, $"Username changed from '{e.PreviousValue}'"),
        UserEmailChangedEvent e => CreateUserEntry(e.User, nameof(UserEmailChangedEvent), ActivityLogCategories.User, $"Email changed from '{e.PreviousValue}'", attributeSubject: true),
        AccountLockedEvent e => CreateUserEntry(e.User, nameof(AccountLockedEvent), ActivityLogCategories.User, "Account locked", attributeSubject: true),
        // Authentication events
        PasswordChangedEvent e => CreateUserEntry(e.User, nameof(PasswordChangedEvent), ActivityLogCategories.Authentication, "Password changed"),
        EmailConfirmedEvent e => CreateUserEntry(e.User, nameof(EmailConfirmedEvent), ActivityLogCategories.Authentication, "Email confirmed", attributeSubject: true),
        PhoneNumberConfirmedEvent e => CreateUserEntry(e.User, nameof(PhoneNumberConfirmedEvent), ActivityLogCategories.Authentication, "Phone number confirmed", attributeSubject: true),
        UserRequestForEmailConfirmationEvent e => CreateUserEntry(e.User, nameof(UserRequestForEmailConfirmationEvent), ActivityLogCategories.Authentication, "Email confirmation requested"),
        // Device events
        DeviceCreatedEvent e => CreateDeviceEntry(e.Device, nameof(DeviceCreatedEvent), "Device created", e.User),
        DeviceUpdatedEvent e => CreateDeviceEntry(e.Device, nameof(DeviceUpdatedEvent), "Device updated", e.User),
        DeviceDeletedEvent e => CreateDeviceEntry(e.Device, nameof(DeviceDeletedEvent), "Device deleted"),
        DeviceTrustRequestedEvent e => CreateDeviceEntry(e.Device, nameof(DeviceTrustRequestedEvent), "Device trust requested", e.User),
        // Client events
        ClientCreatedEvent e => CreateClientEntry(e.Client, nameof(ClientCreatedEvent), "Client created"),
        ClientUpdatedEvent e => CreateClientEntry(e.Client, nameof(ClientUpdatedEvent), "Client updated"),
        ClientDeletedEvent e => CreateClientEntry(e.Client, nameof(ClientDeletedEvent), "Client deleted"),
        _ => null
    };

    // For events whose actor is always the affected user (always-anonymous or always-self-service),
    // set the subject from the event context. This is safe only because the converter runs before
    // UserInfoEnricher (which uses ??=), so it must NOT be used for admin-capable events.
    private static ActivityLogEntry CreateUserEntry(UserEventContext user, string eventType, string category, string description, bool attributeSubject = false) => new() {
        EventType = eventType,
        Category = category,
        ResourceId = user.Id,
        ResourceType = "User",
        Description = description,
        Succeeded = true,
        SubjectId = attributeSubject ? user.Id : null,
        SubjectName = attributeSubject ? user.UserName : null
    };

    private static ActivityLogEntry CreateDeviceEntry(UserDeviceEventContext device, string eventType, string description, UserEventContext? owner = null) => new() {
        EventType = eventType,
        Category = ActivityLogCategories.Device,
        ResourceId = device.DeviceId,
        ResourceType = "Device",
        DeviceId = device.DeviceId,
        Description = $"{description}: {device.Name ?? device.Model ?? device.DeviceId}",
        Succeeded = true,
        SubjectId = owner?.Id,
        SubjectName = owner?.UserName
    };

    private static ActivityLogEntry CreateClientEntry(ClientEventContext client, string eventType, string description) => new() {
        EventType = eventType,
        Category = ActivityLogCategories.Client,
        ResourceId = client.ClientId,
        ResourceType = "Client",
        Description = $"{description}: {client.ClientName ?? client.ClientId}",
        Succeeded = true
    };

}