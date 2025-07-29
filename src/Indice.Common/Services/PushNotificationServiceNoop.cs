using Indice.Types;

namespace Indice.Services;

/// <summary>Default push notification service implementation for clients who don't support it.</summary>
public class PushNotificationServiceNoop : IPushNotificationService
{
    ///<inheritdoc/>
    public Task Register(string deviceId, string? pnsHandle, DevicePlatform devicePlatform, IList<PushNotificationTag> tags) => Task.CompletedTask;

    ///<inheritdoc/>
    public Task<SendReceipt> SendAsync(string title, string? body, IList<PushNotificationTag>? tags, string? data = null, string? classification = null) => Task.FromResult(new SendReceipt(string.Empty, DateTimeOffset.UtcNow));

    ///<inheritdoc/>
    public Task UnRegister(string deviceId) => Task.CompletedTask;
}
