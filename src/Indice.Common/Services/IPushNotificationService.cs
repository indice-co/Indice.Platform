using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indice.Types;

namespace Indice.Services
{
    /// <summary>
    /// Push notification service abstraction in order to support different providers
    /// </summary>
    public interface IPushNotificationService
    {
        /// <summary>
        /// Register a device to azure notification hub
        /// </summary>
        /// <param name="deviceId">The device id to register</param>
        /// <param name="pnsHandle">Platform Notification Service(pns) obtained from client platform</param>
        /// <param name="devicePlatform">Client device platform</param>
        /// <param name="tags">Tags are used to route notifications to the correct set of device handles</param>
        Task Register(string deviceId, string pnsHandle, DevicePlatform devicePlatform, IList<string> tags);

        /// <summary>
        /// Unregister device from receiving notifications
        /// </summary>
        /// <param name="deviceId">The device id to unregister</param>
        Task UnRegister(string deviceId);

        /// <summary>
        /// Send notifications to specified tags
        /// </summary>
        /// <param name="message">Message of notification</param>
        /// <param name="tags">Tags are used to route notifications to the correct set of device handles</param>
        /// <param name="data">Data passed to mobile client, not visible to notification toast</param>
        Task SendAsync(string message, IList<string> tags, string data = null);
    }

    /// <summary>
    /// Extensions for <see cref="IPushNotificationService"/>
    /// </summary>
    public static class PushNotificationServiceExtensions
    {
        /// <summary>
        /// Register a device for userId
        /// </summary>
        /// <param name="service">Instance of <see cref="IPushNotificationService"/></param>
        /// <param name="deviceId">DeviceId to register</param>
        /// <param name="pnsHandle">Platform Notification Service(pns) obtained from client platform</param>
        /// <param name="devicePlatform">Client device platform</param>
        /// <param name="userId">UserId to be passed as tag</param>
        /// <param name="tags">Optional tag parameters</param>
        public static async Task Register(this IPushNotificationService service, string deviceId, string pnsHandle, DevicePlatform devicePlatform, string userId, params string[] tags) {
            await service.Register(deviceId, pnsHandle, devicePlatform, new string[] { userId }.Concat(tags).ToList());
        }

        /// <summary>
        /// Send notifications to devices registered to userId
        /// </summary>
        /// <param name="service">Instance of <see cref="IPushNotificationService"/></param>
        /// <param name="message">Message of notification</param>
        /// <param name="userId">UserId to be passed as tag</param>
        /// <param name="tags">Optional tag parameters</param>
        public static async Task SendAsync(this IPushNotificationService service, string message, string userId, params string[] tags) {
            await service.SendAsync(message, new string[] { userId }.Concat(tags).ToList());
        }

        /// <summary>
        /// Send notifications to devices registered to userId
        /// </summary>
        /// <param name="service">Instance of <see cref="IPushNotificationService"/></param>
        /// <param name="message">Message of notification</param>
        /// <param name="data">Data passed to mobile client, not visible to notification toast</param>
        /// <param name="userId">UserId to be passed as tag</param>
        /// <param name="tags">Optional tag parameters</param>
        public static async Task SendAsync(this IPushNotificationService service, string message, string data, string userId, params string[] tags) {
            await service.SendAsync(message, new string[] { userId }.Concat(tags).ToList(), data);
        }
    }

    /// <summary>
    /// Default push notification service implementation for clients who don't support it
    /// </summary>
    public class DefaultPushNotificationService : IPushNotificationService
    {
        ///<inheritdoc/>
        public Task Register(string deviceId, string pnsHandle, DevicePlatform devicePlatform, IList<string> tags) {
            throw new System.NotImplementedException();
        }

        ///<inheritdoc/>
        public Task SendAsync(string message, IList<string> tags, string data = null) {
            throw new System.NotImplementedException();
        }

        ///<inheritdoc/>
        public Task UnRegister(string deviceId) {
            throw new System.NotImplementedException();
        }
    }
}
