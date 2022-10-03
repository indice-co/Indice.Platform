using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Indice.Serialization;
using Indice.Types;

namespace Indice.Services
{
    /// <summary>Push notification service abstraction in order to support different providers.</summary>
    public interface IPushNotificationService
    {
        /// <summary>Register a device to Azure notification hub.</summary>
        /// <param name="deviceId">The device id to register.</param>
        /// <param name="pnsHandle">Platform Notification Service (PNS) obtained from client platform.</param>
        /// <param name="devicePlatform">Client device platform.</param>
        /// <param name="tags">Tags are used to route notifications to the correct set of device handles.</param>
        Task Register(string deviceId, string pnsHandle, DevicePlatform devicePlatform, IList<string> tags);
        /// <summary>Unregister device from receiving notifications.</summary>
        /// <param name="deviceId">The device id to unregister.</param>
        Task UnRegister(string deviceId);
        /// <summary>Send notifications to specified tags.</summary>
        /// <param name="title">Title of notification.</param>
        /// <param name="body">Body of notification.</param>
        /// <param name="tags">Tags are used to route notifications to the correct set of device handles.</param>
        /// <param name="data">Data passed to mobile client, not visible to notification toast.</param>
        /// <param name="classification">The notification's type.</param>
        Task SendAsync(string title, string body, IList<PushNotificationTag> tags, string data = null, string classification = null);
    }

    /// <summary>Extensions for <see cref="IPushNotificationService"/>.</summary>
    public static class PushNotificationServiceExtensions
    {
        /// <summary>Register a device for userId.</summary>
        /// <param name="service">Instance of <see cref="IPushNotificationService"/>.</param>
        /// <param name="deviceId">DeviceId to register.</param>
        /// <param name="pnsHandle">Platform notification service (PNS) obtained from client platform.</param>
        /// <param name="devicePlatform">Client device platform.</param>
        /// <param name="userTag">UserId to be passed as tag.</param>
        /// <param name="tags">Optional tag parameters.</param>
        public static Task Register(this IPushNotificationService service, string deviceId, string pnsHandle, DevicePlatform devicePlatform, string userTag, params string[] tags) =>
            service.Register(deviceId, pnsHandle, devicePlatform, new string[] { userTag }.Concat(tags ?? Array.Empty<string>()).ToList());

        /// <summary>Send notifications to devices registered to userId with payload data and classification.</summary>
        /// <param name="service">Instance of <see cref="IPushNotificationService"/>.</param>
        /// <param name="title">Message of notification.</param>
        /// <param name="body">Body of notification.</param>
        /// <param name="data">Data passed to mobile client, not visible to notification toast.</param>
        /// <param name="userTag">UserId to be passed as tag.</param>
        /// <param name="classification">The type of the Push Notification.</param>
        /// <param name="tags">Optional tag parameters.</param>
        public static Task SendAsync(this IPushNotificationService service, string title, string body, string data, string userTag, string classification = null, params string[] tags) =>
            service.SendAsync(title, body, new string[] { userTag }.Concat(tags ?? Array.Empty<string>()).ToList(), data, classification);

        /// <summary>Send notifications to devices registered to userId with payload data and classification.</summary>
        /// <typeparam name="TData">The type of data sent in the notification payload.</typeparam>
        /// <param name="service">Instance of <see cref="IPushNotificationService"/>.</param>
        /// <param name="title">Message of notification.</param>
        /// <param name="body">Body of notification.</param>
        /// <param name="data">Data passed to mobile client, not visible to notification toast.</param>
        /// <param name="userTag">UserId to be passed as tag.</param>
        /// <param name="classification">The type of the Push Notification.</param>
        /// <param name="tags">Optional tag parameters.</param>
        public static Task SendAsync<TData>(this IPushNotificationService service, string title, string body, TData data, string userTag, string classification = null, params string[] tags) where TData : class =>
            service.SendAsync(title, body, data != null ? JsonSerializer.Serialize(data, JsonSerializerOptionDefaults.GetDefaultSettings()) : null, userTag, classification, tags);

        /// <summary>Sends a notification directly to the device specified by it's unique id.</summary>
        /// <param name="service">Instance of <see cref="IPushNotificationService"/>.</param>
        /// <param name="deviceId">The id of the device.</param>
        /// <param name="title">Message of notification.</param>
        /// <param name="body">Body of notification.</param>
        /// <param name="data">Data passed to mobile client, not visible to notification toast.</param>
        /// <param name="classification">The type of the Push Notification.</param>
        public static Task SendToDeviceAsync(this IPushNotificationService service, string deviceId, string title, string body, string data = null, string classification = null) => 
            service.SendAsync(title, body, new List<PushNotificationTag> { new PushNotificationTag(deviceId, PushNotificationTagReferral.Device) }, data, classification);

        /// <summary>Send notification to devices registered to userId with optional data as payload.</summary>
        /// <param name="service">Instance of <see cref="IPushNotificationService"/>.</param>
        /// <param name="configurePushNotificationMessage">The delegate that will be used to build the <see cref="PushNotificationMessage"/>.</param>
        public static async Task SendAsync(this IPushNotificationService service, Action<PushNotificationMessageBuilder> configurePushNotificationMessage) {
            if (configurePushNotificationMessage == null) {
                throw new ArgumentNullException(nameof(configurePushNotificationMessage));
            }
            var builder = new PushNotificationMessageBuilder();
            configurePushNotificationMessage.Invoke(builder);
            var pushNotificationMessage = builder.Build();
            await service.SendAsync(pushNotificationMessage.Title, pushNotificationMessage.Body, pushNotificationMessage.Data, pushNotificationMessage.UserTag, pushNotificationMessage.Classification, pushNotificationMessage.Tags.ToArray());
        }

        /// <summary>Sends a notification to all registered devices.</summary>
        /// <param name="service">Instance of <see cref="IPushNotificationService"/>.</param>
        /// <param name="title">Message of notification.</param>
        /// <param name="body">Body of notification.</param>
        /// <param name="data">Data passed to mobile client, not visible to notification toast.</param>
        /// <param name="classification">The type of the Push Notification.</param>
        public static Task BroadcastAsync(this IPushNotificationService service, string title, string body, string data, string classification = null) =>
            service.SendAsync(title, body, new List<string>(), data, classification);

        /// <summary>Sends a notification to all registered devices.</summary>
        /// <typeparam name="TData">The type of data sent in the notification payload.</typeparam>
        /// <param name="service">Instance of <see cref="IPushNotificationService"/>.</param>
        /// <param name="title">Message of notification.</param>
        /// <param name="body">Body of notification.</param>
        /// <param name="data">Data passed to mobile client, not visible to notification toast.</param>
        /// <param name="classification">The type of the Push Notification.</param>
        public static Task BroadcastAsync<TData>(this IPushNotificationService service, string title, string body, TData data, string classification = null) where TData : class =>
            service.BroadcastAsync(title, body, data != null ? JsonSerializer.Serialize(data, JsonSerializerOptionDefaults.GetDefaultSettings()) : null, classification);
    }
}
