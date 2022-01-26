using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Indice.Types;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Services
{
    /// <summary>
    /// Push notification service implementation using <see cref="Microsoft.Azure.NotificationHubs"/>.
    /// </summary>
    public class PushNotificationServiceAzure : IPushNotificationService
    {
        /// <summary>
        /// Windows phone template.
        /// </summary>
        private const string WindowsPhoneTemplate = @"<toast><visual><binding template=""ToastText01""><text id=""1"">$(message)</text><text id=""1"">$(data)</text></binding></visual></toast>";
        /// <summary>
        /// iOS template.
        /// </summary>
        private const string IosTemplate = @"{""aps"":{""alert"":""$(message)"", ""category"":""$(classification)""}, ""payload"":{""data"":""$(data)""}}";
        /// <summary>
        /// Android template.
        /// </summary>
        private const string AndroidTemplate = @"{""data"":{""message"":""$(message)"", ""data"":""$(data)"", ""category"":""$(classification)""}}";
        /// <summary>
        /// The connection string parameter name. The setting key that will be searched inside the configuration.
        /// </summary>
        public const string ConnectionStringName = "PushNotificationsConnection";
        /// <summary>
        /// The push notification hub path string parameter name. The setting key that will be searched inside the configuration.
        /// </summary>
        public const string NotificationsHubPath = "PushNotificationsHubPath";

        /// <summary>
        /// Constructs the <see cref="PushNotificationServiceAzure"/>.
        /// </summary>
        /// <param name="options">Connection string for Azure and Notifications hub name.</param>
        public PushNotificationServiceAzure(PushNotificationAzureOptions options) {
            if (string.IsNullOrWhiteSpace(options?.ConnectionString) || string.IsNullOrWhiteSpace(options?.NotificationHubPath)) {
                throw new InvalidOperationException($"{nameof(PushNotificationAzureOptions)} are not properly configured.");
            }
            NotificationHub = new NotificationHubClient(
                options.ConnectionString,
                options.NotificationHubPath,
                options.MessageHandler != null ? new NotificationHubSettings { MessageHandler = options.MessageHandler } : null
            );
        }

        /// <summary>
        /// Notifications hub instance.
        /// </summary>
        private NotificationHubClient NotificationHub { get; }

        /// <inheritdoc/>
        public async Task Register(string deviceId, string pnsHandle, DevicePlatform devicePlatform, IList<string> tags) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            if (!(tags?.Count > 0)) {
                throw new ArgumentException("Tags list is empty.");
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
                    installationRequest.Templates.Add("DefaultMessage", new InstallationTemplate {
                        Body = WindowsPhoneTemplate
                    });
                    break;
                case DevicePlatform.UWP:
                    installationRequest.Platform = NotificationPlatform.Wns;
                    installationRequest.Templates.Add("DefaultMessage", new InstallationTemplate {
                        Body = WindowsPhoneTemplate
                    });
                    break;
                case DevicePlatform.iOS:
                    installationRequest.Platform = NotificationPlatform.Apns;
                    installationRequest.Templates.Add("DefaultMessage", new InstallationTemplate {
                        Body = IosTemplate
                    });
                    break;
                case DevicePlatform.Android:
                    installationRequest.Platform = NotificationPlatform.Fcm;
                    installationRequest.Templates.Add("DefaultMessage", new InstallationTemplate {
                        Body = AndroidTemplate
                    });
                    break;
                default:
                    throw new ArgumentException("Device platform not supported.", nameof(devicePlatform));
            }
            await NotificationHub.CreateOrUpdateInstallationAsync(installationRequest);
        }

        /// <inheritdoc/>
        public async Task UnRegister(string deviceId) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentNullException(nameof(deviceId));
            }
            await NotificationHub.DeleteInstallationAsync(deviceId);
        }

        /// <inheritdoc/>
        public async Task SendAsync(string message, IList<string> tags, string data = null, string classification = null) {
            if (string.IsNullOrEmpty(message)) {
                throw new ArgumentNullException(nameof(message));
            }
            var notification = new Dictionary<string, string> {
                { "message", message }
            };
            if (!string.IsNullOrEmpty(data)) {
                notification.Add("data", data);
            }
            if (!string.IsNullOrEmpty(classification)) {
                notification.Add("classification", classification);
            }
            if (tags?.Any() == true) {
                await NotificationHub.SendTemplateNotificationAsync(notification, tags);
            } else {
                await NotificationHub.SendTemplateNotificationAsync(notification);
            }
        }
    }

    /// <summary>
    /// Push notification service options.
    /// </summary>
    public class PushNotificationAzureOptions
    {
        internal IServiceCollection Services { get; set; }
        /// <summary>
        /// The section name in application settings.
        /// </summary>
        public const string Name = "PushNotifications";
        /// <summary>
        /// The connection string to the push notification account.
        /// </summary>
        public string ConnectionString { get; set; }
        /// <summary>
        /// Notifications hub name, if any.
        /// </summary>
        public string NotificationHubPath { get; set; }
        /// <summary>
        /// Gets or sets HTTP message handler.
        /// </summary>
        public HttpClientHandler MessageHandler { get; set; }
    }
}
