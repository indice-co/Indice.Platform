using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Indice.Types;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Extensions.Logging;

namespace Indice.Services
{
    /// <summary>
    /// Push notification service implementation using <see cref="Microsoft.Azure.NotificationHubs"/>.
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
        /// Windows phone template.
        /// </summary>
        private readonly string WindowsPhoneTemplate = @"<toast><visual><binding template=""ToastText01""><text id=""1"">$(message)</text><text id=""1"">$(data)</text></binding></visual></toast>";
        /// <summary>
        /// iOS template.
        /// </summary>
        private readonly string IOSTemplate = @"{""aps"":{""alert"":""$(message)""}, ""payload"":{""data"":""$(data)""}}";
        /// <summary>
        /// Android template.
        /// </summary>
        private readonly string AndroidTemplate = @"{""data"":{""message"":""$(message)"", ""data"":""$(data)""}}";
        /// <summary>
        /// Notifications hub instance.
        /// </summary>
        protected NotificationHubClient NotificationHub { get; }
        /// <summary>
        /// Represents a type used to perform logging.
        /// </summary>
        protected ILogger<PushNotificationServiceAzure> Logger { get; }

        /// <summary>
        /// Constructs the <see cref="PushNotificationServiceAzure"/>.
        /// </summary>
        /// <param name="options">Connection string for azure and Notifications hub name.</param>
        public PushNotificationServiceAzure(PushNotificationOptions options) {
            if (string.IsNullOrEmpty(options.ConnectionString)) {
                throw new ArgumentNullException(nameof(options.ConnectionString));
            }
            if (string.IsNullOrEmpty(options.NotificationHubPath)) {
                throw new ArgumentNullException(nameof(options.NotificationHubPath));
            }
            NotificationHub = NotificationHubClient.CreateClientFromConnectionString(options.ConnectionString, options.NotificationHubPath);
        }

        ///<inheritdoc/>
        public async Task Register(string deviceId, string pnsHandle, DevicePlatform devicePlatform, IList<string> tags) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            if (!(tags?.Count > 0)) {
                throw new ArgumentException("Tags list is empty");
            }
            var installationRequest = new Installation {
                InstallationId = deviceId,
                PushChannel = pnsHandle,
                Tags = tags,
                Templates = new Dictionary<string, InstallationTemplate>()
            };
            switch (devicePlatform) {
                case DevicePlatform.WindowsPhone:
                    installationRequest.Platform = NotificationPlatform.Mpns;
                    installationRequest.Templates.Add(
                        "DefaultMessage", new InstallationTemplate { Body = WindowsPhoneTemplate }
                    );
                    break;
                case DevicePlatform.UWP:
                    installationRequest.Platform = NotificationPlatform.Wns;
                    installationRequest.Templates.Add(
                        "DefaultMessage", new InstallationTemplate { Body = WindowsPhoneTemplate }
                    );
                    break;
                case DevicePlatform.iOS:
                    installationRequest.Platform = NotificationPlatform.Apns;
                    installationRequest.Templates.Add(
                        "DefaultMessage", new InstallationTemplate { Body = IOSTemplate }
                    );
                    break;
                case DevicePlatform.Android:
                    installationRequest.Platform = NotificationPlatform.Fcm;
                    installationRequest.Templates.Add(
                        "DefaultMessage", new InstallationTemplate { Body = AndroidTemplate }
                    );
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
        public async Task SendAsync(string message, IList<string> tags, string data = null) {
            if (string.IsNullOrEmpty(message)) {
                throw new ArgumentNullException(nameof(message));
            }
            if (!(tags?.Count > 0)) {
                throw new ArgumentException("Tags list is empty");
            }
            var notification = new Dictionary<string, string> {
                { "message", message }
            };
            if (!string.IsNullOrEmpty(data)) {
                notification.Add("data", data);
            }
            await NotificationHub.SendTemplateNotificationAsync(notification, tags);
        }
    }

    /// <summary>
    /// Push notification service options specific to Azure.
    /// </summary>
    public class PushNotificationOptions
    {
        /// <summary>
        /// The section name in application settings.
        /// </summary>
        public const string Name = "PushNotifications";
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
