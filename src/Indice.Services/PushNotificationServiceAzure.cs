﻿using Indice.Types;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Indice.Services;

/// <summary>Push notification service implementation using <see cref="Microsoft.Azure.NotificationHubs"/>.</summary>
public class PushNotificationServiceAzure : IPushNotificationService
{
    private readonly ILogger _logger;
    /// <summary>The connection string parameter name. The setting key that will be searched inside the configuration.</summary>
    public const string ConnectionStringName = "PushNotificationsConnection";
    /// <summary>The push notification hub path string parameter name. The setting key that will be searched inside the configuration.</summary>
    public const string NotificationsHubPath = "PushNotificationsHubPath";

    /// <summary>Constructs the <see cref="PushNotificationServiceAzure"/>.</summary>
    /// <param name="options">Connection string for Azure and Notifications hub name.</param>
    /// <param name="loggerFactory">Represents a type used to configure the logging system and create instances of <see cref="ILogger"/> from the registered <see cref="ILoggerProvider"/>s.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public PushNotificationServiceAzure(PushNotificationAzureOptions options, ILoggerFactory loggerFactory) {
        if (string.IsNullOrWhiteSpace(options?.ConnectionString) || string.IsNullOrWhiteSpace(options?.NotificationHubPath)) {
            throw new InvalidOperationException($"{nameof(PushNotificationAzureOptions)} are not properly configured.");
        }
        PushNotificationAzureOptions = options;
        NotificationHub = new NotificationHubClient(
            PushNotificationAzureOptions.ConnectionString,
            PushNotificationAzureOptions.NotificationHubPath,
            PushNotificationAzureOptions.MessageHandler is not null
                ? new NotificationHubSettings { MessageHandler = PushNotificationAzureOptions.MessageHandler }
                : null
        );
        _logger = loggerFactory.CreateLogger(nameof(PushNotificationServiceAzure));
    }

    private NotificationHubClient NotificationHub { get; }
    private PushNotificationAzureOptions PushNotificationAzureOptions { get; }

    /// <inheritdoc/>
    public async Task Register(string deviceId, string pnsHandle, DevicePlatform devicePlatform, IList<PushNotificationTag> tags) {
        if (string.IsNullOrEmpty(deviceId)) {
            throw new ArgumentNullException(nameof(deviceId));
        }
        if (!(tags?.Count > 0)) {
            throw new ArgumentException("Tags list is empty.");
        }
        var installationRequest = new Installation {
            InstallationId = deviceId,
            PushChannel = pnsHandle,
            Tags = tags.Select(tag => tag.ToString()).ToList(),
            Templates = new Dictionary<string, InstallationTemplate>()
        };
        switch (devicePlatform) {
            case DevicePlatform.iOS:
                installationRequest.Platform = NotificationPlatform.Apns;
                installationRequest.Templates.Add("DefaultMessage", new InstallationTemplate {
                    Body = PushNotificationAzureOptions.SilentNotifications ?? false
                        ? PushNotificationServiceAzureTemplates.Silent.IOS
                        : PushNotificationServiceAzureTemplates.Generic.IOS
                });
                break;
            case DevicePlatform.Android:
                installationRequest.Platform = NotificationPlatform.FcmV1;
                installationRequest.Templates.Add("DefaultMessage", new InstallationTemplate {
                    Body = PushNotificationAzureOptions.SilentNotifications ?? true 
                        ? PushNotificationServiceAzureTemplates.Silent.ANDROID 
                        : PushNotificationServiceAzureTemplates.Generic.ANDROID
                });
                break;
            default:
                throw new ArgumentException("Device platform not supported.", nameof(devicePlatform));
        }
        await NotificationHub.CreateOrUpdateInstallationAsync(installationRequest);
        _logger.LogInformation("{Platform} device with id '{Id}' was registered to Azure Notification Hubs with the following tags: {Tags}.", devicePlatform, deviceId, string.Join(", ", tags));
    }

    /// <inheritdoc/>
    public async Task UnRegister(string deviceId) {
        if (string.IsNullOrEmpty(deviceId)) {
            throw new ArgumentNullException(nameof(deviceId));
        }
        await NotificationHub.DeleteInstallationAsync(deviceId);
        _logger.LogInformation("Device with id '{DeviceId}' was deleted from Azure Notification Hubs.", deviceId);
    }

    /// <inheritdoc/>
    public async Task SendAsync(string title, string body, IList<PushNotificationTag> tags, string data = null, string classification = null) {
        if (string.IsNullOrEmpty(title)) {
            throw new ArgumentNullException(nameof(title));
        }
        var properties = new Dictionary<string, string> {
            { "message", title }
        };
        if (!string.IsNullOrWhiteSpace(body)) {
            properties.Add("body", body);
        }
        if (!string.IsNullOrEmpty(data)) {
            properties.Add("data", data);
        }
        if (!string.IsNullOrEmpty(classification)) {
            properties.Add("classification", classification);
        }
        if (tags?.Any() == true) {
            var tagsCollection = tags.Select(
                tag => tag.Kind == PushNotificationTagKind.User || tag.Kind == PushNotificationTagKind.Unspecified
                    ? tag.ToString()
                    : "$InstallationId:{" + tag.Value + "}" // https://learn.microsoft.com/en-us/azure/notification-hubs/notification-hubs-push-notification-registration-management#installations
            );
            await NotificationHub.SendTemplateNotificationAsync(properties, tagsCollection);
            _logger.LogInformation("A push notification was dispatched to Azure Notification Hubs with properties '{Properties}' to the following tags: {Tags}.", string.Join(" - ", properties), string.Join(", ", tags));
        } else {
            await NotificationHub.SendTemplateNotificationAsync(properties);
            _logger.LogInformation("A push notification was dispatched to Azure Notification Hubs with properties {Properties}.", string.Join(" - ", properties));
        }
    }
}

/// <summary>Push notification service options.</summary>
public class PushNotificationAzureOptions
{
    internal IServiceCollection Services { get; set; }
    /// <summary>The section name in application settings.</summary>
    public const string Name = "PushNotifications";
    /// <summary>The connection string to the push notification account.</summary>
    public string ConnectionString { get; set; }
    /// <summary>Notifications hub name, if any.</summary>
    public string NotificationHubPath { get; set; }
    /// <summary>Specifies whether the notification is handled by the operating system.</summary>
    public bool? SilentNotifications { get; set; }
    /// <summary>Gets or sets HTTP message handler.</summary>
    public HttpClientHandler MessageHandler { get; set; }
}

/// <summary>Generic and silent push notification templates for Android and iOS.</summary>
public class PushNotificationServiceAzureTemplates
{
    /// <summary>Generic templates.</summary>
    public class Generic
    {
        /// <summary>iOS generic template.</summary>
        public const string IOS = """
            {
                "aps":{
                    "alert": {
                        "title": "$(message)",
            			"body": "$(body)"
                    }, 
                    "category": "$(classification)"
                }, 
                "payload":{
                    "message": "$(message)", 
                    "data": "$(data)", 
                    "category": "$(classification)"
                }
            }
            """;

        /// <summary>Android generic template.</summary>
        public const string ANDROID = """
            {
            	"message": {
            		"notification": {
            			"title": "$(message)",
            			"body": "$(body)",
            		},
            		"data": {
            			"message": "$(message)",
            			"data": "$(data)",
            			"category": "$(classification)",
                        "body": "$(body)"
            		}
            	}
            }
            """;
    }

    /// <summary>Silent templates.</summary>
    public class Silent
    {
        /// <summary>iOS silent template.</summary>
        public const string IOS = """
            {
                "aps": { 
                    "category": "$(classification)",
                    "content-available": 1,
                    "apns-priority": 5,
                    "sound": "",
                    "badge": 0
                },              
                "payload":{
                    "message": "$(message)", 
                    "data": "$(data)", 
                    "category": "$(classification)",
                    "body": "$(body)"
                }
            }
            """;

        /// <summary>Android silent template.</summary>
        public const string ANDROID = """
            {
            	"message": {
            		"data": {
            			"message": "$(message)",
            			"data": "$(data)",
            			"category": "$(classification)",
                        "body": "$(body)"
            		}
            	}
            }
            """;
    }
}
