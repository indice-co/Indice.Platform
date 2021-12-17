using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Indice.Serialization;
using Indice.Types;

namespace Indice.Services
{
    /// <summary>
    /// Push notification service abstraction in order to support different providers.
    /// </summary>
    public interface IPushNotificationService
    {
        /// <summary>
        /// Register a device to Azure notification hub.
        /// </summary>
        /// <param name="deviceId">The device id to register.</param>
        /// <param name="pnsHandle">Platform Notification Service(pns) obtained from client platform.</param>
        /// <param name="devicePlatform">Client device platform.</param>
        /// <param name="tags">Tags are used to route notifications to the correct set of device handles.</param>
        Task Register(string deviceId, string pnsHandle, DevicePlatform devicePlatform, IList<string> tags);
        /// <summary>
        /// Unregister device from receiving notifications.
        /// </summary>
        /// <param name="deviceId">The device id to unregister.</param>
        Task UnRegister(string deviceId);
        /// <summary>
        /// Send notifications to specified tags.
        /// </summary>
        /// <param name="message">Message of notification.</param>
        /// <param name="tags">Tags are used to route notifications to the correct set of device handles.</param>
        /// <param name="data">Data passed to mobile client, not visible to notification toast.</param>
        /// <param name="classification">The notification's type.</param>
        Task SendAsync(string message, IList<string> tags, string data = null, string classification = null);
    }

    /// <summary>
    /// Extensions for <see cref="IPushNotificationService"/>.
    /// </summary>
    public static class PushNotificationServiceExtensions
    {
        /// <summary>
        /// Register a device for userId.
        /// </summary>
        /// <param name="service">Instance of <see cref="IPushNotificationService"/>.</param>
        /// <param name="deviceId">DeviceId to register.</param>
        /// <param name="pnsHandle">Platform notification service (pns) obtained from client platform.</param>
        /// <param name="devicePlatform">Client device platform.</param>
        /// <param name="userId">UserId to be passed as tag.</param>
        /// <param name="tags">Optional tag parameters.</param>
        public static async Task Register(this IPushNotificationService service, string deviceId, string pnsHandle, DevicePlatform devicePlatform, string userId, params string[] tags) =>
            await service.Register(deviceId, pnsHandle, devicePlatform, new string[] { userId }.Concat(tags ?? Array.Empty<string>()).ToList());

        /// <summary>
        /// Send notifications to devices registered to userId with payload data and classification.
        /// </summary>
        /// <param name="service">Instance of <see cref="IPushNotificationService"/>.</param>
        /// <param name="message">Message of notification.</param>
        /// <param name="data">Data passed to mobile client, not visible to notification toast.</param>
        /// <param name="userId">UserId to be passed as tag.</param>
        /// <param name="classification">The type of the Push Notification.</param>
        /// <param name="tags">Optional tag parameters.</param>
        public static async Task SendAsync(this IPushNotificationService service, string message, string data, string userId, string classification = null, params string[] tags) =>
            await service.SendAsync(message, new string[] { userId }.Concat(tags ?? Array.Empty<string>()).ToList(), data, classification);

        /// <summary>
        /// Send notifications to devices registered to userId with payload data and classification.
        /// </summary>
        /// <typeparam name="TData">The type of data sent in the notification payload.</typeparam>
        /// <param name="service">Instance of <see cref="IPushNotificationService"/>.</param>
        /// <param name="message">Message of notification.</param>
        /// <param name="data">Data passed to mobile client, not visible to notification toast.</param>
        /// <param name="userId">UserId to be passed as tag.</param>
        /// <param name="classification">The type of the Push Notification.</param>
        /// <param name="tags">Optional tag parameters.</param>
        public static async Task SendAsync<TData>(this IPushNotificationService service, string message, TData data, string userId, string classification = null, params string[] tags) where TData : class =>
            await service.SendAsync(message, JsonSerializer.Serialize(data, JsonSerializerOptionDefaults.GetDefaultSettings()), userId, classification, tags);

        /// <summary>
        /// Sends a notification to all registered devices.
        /// </summary>
        /// <param name="service">Instance of <see cref="IPushNotificationService"/>.</param>
        /// <param name="message">Message of notification.</param>
        /// <param name="data">Data passed to mobile client, not visible to notification toast.</param>
        /// <param name="classification">The type of the Push Notification.</param>
        public static async Task BroadcastAsync(this IPushNotificationService service, string message, string data, string classification = null) =>
            await service.SendAsync(message, new List<string>(), data, classification);

        /// <summary>
        /// Sends a notification to all registered devices.
        /// </summary>
        /// <typeparam name="TData">The type of data sent in the notification payload.</typeparam>
        /// <param name="service">Instance of <see cref="IPushNotificationService"/>.</param>
        /// <param name="message">Message of notification.</param>
        /// <param name="data">Data passed to mobile client, not visible to notification toast.</param>
        /// <param name="classification">The type of the Push Notification.</param>
        public static async Task BroadcastAsync<TData>(this IPushNotificationService service, string message, TData data, string classification = null) where TData : class =>
            await service.BroadcastAsync(message, data != null ? JsonSerializer.Serialize(data, JsonSerializerOptionDefaults.GetDefaultSettings()) : null, classification);

        /// <summary>
        /// Send notification to devices registered to userId with optional data as payload.
        /// </summary>
        /// <param name="service">Instance of <see cref="IPushNotificationService"/>.</param>
        /// <param name="configurePushNotificationMessage">The delegate that will be used to build the <see cref="PushNotificationMessage"/>.</param>
        /// <returns></returns>
        public static async Task SendAsync(this IPushNotificationService service, Func<PushNotificationMessageBuilder, PushNotificationMessageBuilder> configurePushNotificationMessage) {
            if (configurePushNotificationMessage == null) {
                throw new ArgumentNullException(nameof(configurePushNotificationMessage));
            }
            var pushNotificationMessageBuilder = configurePushNotificationMessage(new PushNotificationMessageBuilder());
            var pushNotificationMessage = pushNotificationMessageBuilder.Build();
            await service.SendAsync(pushNotificationMessage.Message, pushNotificationMessage.Data, pushNotificationMessage.UserId, pushNotificationMessage.Classification, pushNotificationMessage.Tags.ToArray());
        }
    }

    /// <summary>
    /// Default push notification service implementation for clients who don't support it.
    /// </summary>
    public class DefaultPushNotificationService : IPushNotificationService
    {
        ///<inheritdoc/>
        public Task Register(string deviceId, string pnsHandle, DevicePlatform devicePlatform, IList<string> tags) => Task.CompletedTask;

        ///<inheritdoc/>
        public Task SendAsync(string message, IList<string> tags, string data = null, string classification = null) => Task.CompletedTask;

        ///<inheritdoc/>
        public Task UnRegister(string deviceId) => Task.CompletedTask;
    }
}
