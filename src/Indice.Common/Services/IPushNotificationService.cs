using System.Collections.Generic;
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
        /// <param name="pnsHandle">Platform Notification Service(pns) obtained from client app</param>
        /// <param name="tags">Tags are used to route notifications to the correct set of device handles</param>
        /// <param name="devicePlatform">Client device platform</param>
        /// <returns></returns>
        Task Register(string deviceId, string pnsHandle, IList<string> tags, DevicePlatform devicePlatform);

        /// <summary>
        /// Unregister device from receiving notifications
        /// </summary>
        /// <param name="deviceId">The device id to unregister</param>
        /// <returns></returns>
        Task UnRegister(string deviceId);

        /// <summary>
        /// Send notifications to specified tags
        /// </summary>
        /// <param name="message">Message of notifications</param>
        /// <param name="tags">Tags are used to route notifications to the correct set of device handles</param>
        /// <param name="devicePlatform">Client device platform</param>
        /// <returns></returns>
        Task SendAsync(string message, IList<string> tags, DevicePlatform devicePlatform);
    }
}
