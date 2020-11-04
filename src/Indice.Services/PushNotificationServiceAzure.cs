using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Indice.Types;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Extensions.Logging;

namespace Indice.Services
{
    /// <summary>
    /// Push notification service implementation using Microsoft.Azure.NotificationHubs
    /// </summary>
    public class PushNotificationServiceAzure : IPushNotificationService
    {
        /// <summary>
        /// Notifications hub instance
        /// </summary>
        protected NotificationHubClient NotificationHub { get; }

        /// <summary>
        /// Represents a type used to perform logging.
        /// </summary>
        protected ILogger<PushNotificationServiceAzure> Logger { get; }
        /// <summary>
        /// Constructs the <see cref="PushNotificationServiceAzure"/>
        /// </summary>
        /// <param name="connectionString">Connection string for azure</param>
        /// <param name="notificationHubPath">Notifications hub name</param>
        public PushNotificationServiceAzure(string connectionString, string notificationHubPath) {
            if (string.IsNullOrEmpty(connectionString)) {
                throw new ArgumentNullException(nameof(connectionString));
            }
            if (string.IsNullOrEmpty(notificationHubPath)) {
                throw new ArgumentNullException(nameof(notificationHubPath));
            }
            NotificationHub = NotificationHubClient.CreateClientFromConnectionString(connectionString, notificationHubPath);
        }

        /// <summary>
        /// Register a device to azure notification hub
        /// </summary>
        /// <param name="deviceId">The device id to register</param>
        /// <param name="pnsHandle">Platform Notification Service(pns) obtained from client app</param>
        /// <param name="tags">Tags are used to route notifications to the correct set of device handles</param>
        /// <param name="devicePlatform">Client device platform</param>
        /// <returns></returns>
        public async Task Register(string deviceId, string pnsHandle, IList<string> tags, DevicePlatform devicePlatform) {
            if(string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException("DeviceId is null");
            }
            if (tags.Count == 0) {
                throw new ArgumentException("Tags list is empty");
            }
            var installationRequest = new Installation() {
                InstallationId = deviceId,
                PushChannel = pnsHandle,
                Tags = tags
            };
            switch (devicePlatform) {
                //For windows phone 8 or Windows Phone 8.1 Silverlight applications
                case DevicePlatform.WindowsPhone:
                    installationRequest.Platform = NotificationPlatform.Mpns;
                    break;
                //For universal windows platform applications
                case DevicePlatform.UWP:
                    installationRequest.Platform = NotificationPlatform.Wns;
                    break;
                case DevicePlatform.iOS:
                    installationRequest.Platform = NotificationPlatform.Apns;
                    break;
                case DevicePlatform.Android:
                    installationRequest.Platform = NotificationPlatform.Fcm;
                    break;
                default:
                    throw new ArgumentException("Device platform not supported", nameof(devicePlatform));
            }
            await NotificationHub.CreateOrUpdateInstallationAsync(installationRequest);
        }

        public Task SendAsync() {
            throw new NotImplementedException();
        }

        public Task UnRegister() {
            throw new NotImplementedException();
        }
    }
}
