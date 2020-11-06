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
        /// The connection string parameter name. The setting key that will be searched inside the configuration.
        /// </summary>
        public const string CONNECTION_STRING_NAME = "PushNotificationsConnection";

        /// <summary>
        /// The push notification hub path string parameter name. The setting key that will be searched inside the configuration.
        /// </summary>
        public const string NOTIFICATIONS_HUB_PATH = "PushNotificationsHubPath";

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

        ///<inheritdoc/>
        public async Task Register(string deviceId, string pnsHandle, IList<string> tags, DevicePlatform devicePlatform) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
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
                case DevicePlatform.None:
                default:
                    throw new ArgumentException("Device platform not supported", nameof(devicePlatform));
            }
            await NotificationHub.CreateOrUpdateInstallationAsync(installationRequest);
        }

        ///<inheritdoc/>
        public async Task UnRegister(string deviceId) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            await NotificationHub.DeleteInstallationAsync(deviceId);
        }

        ///<inheritdoc/>
        public async Task SendAsync(string message, IList<string> tags, DevicePlatform devicePlatform) {
            if (string.IsNullOrEmpty(message)) {
                throw new ArgumentNullException(nameof(message));
            }
            if (tags.Count == 0) {
                throw new ArgumentException("Tags list is empty");
            }
            switch (devicePlatform) {
                case DevicePlatform.WindowsPhone:
                    await NotificationHub.SendMpnsNativeNotificationAsync(message, tags);
                    break;
                case DevicePlatform.UWP:
                    await NotificationHub.SendWindowsNativeNotificationAsync(message, tags);
                    break;
                case DevicePlatform.iOS:
                    await NotificationHub.SendAppleNativeNotificationAsync(message, tags);
                    break;
                case DevicePlatform.Android:
                    await NotificationHub.SendFcmNativeNotificationAsync(message, tags);
                    break;
                case DevicePlatform.None:
                default:
                    throw new ArgumentException("Device platform not supported", nameof(devicePlatform));
            }
        }

        /// <summary>
        /// Push notification service options specific to Azure.
        /// </summary>
        public class PushNotificationOptions
        {
            /// <summary>
            /// The connection string to the Azure Push notification account.
            /// </summary>
            public string ConnectionString { get; set; }
            /// <summary>
            /// Notifications hub name
            /// </summary>
            public string NotificationHubPath { get; set; }
        }
    }
}
