using System;
using System.Text.Json;
using Indice.Serialization;

namespace Indice.Services
{
    /// <summary>The builder to construct an instance of <see cref="PushNotificationMessage"/>.</summary>
    public class PushNotificationMessageBuilder
    {
        /// <summary>
        /// The payload data that will be sent to the mobile client (not visible to the push notification Title or Message).
        /// If the data is null then only the token will be sent as data.
        /// </summary>
        public string Data { get; set; }
        /// <summary>The message of the push notification.</summary>
        public string Title { get; set; }
        /// <summary>The body of the push notification.</summary>
        public string Body { get; set; }
        /// <summary>The user identifier that correlates devices with users. This can be any identifier like user id, username, user email, customer code etc.</summary>
        public string UserTag { get; set; }
        /// <summary>The tags of the push notification.</summary>
        public string[] Tags { get; set; } = Array.Empty<string>();
        /// <summary>The type of the push notification.</summary>
        public string Classification { get; set; }
    }

    /// <summary><see cref="PushNotificationMessageBuilder"/> extensions.</summary>
    public static class PushNotificationMessageBuilderExtensions
    {
        /// <summary>Defines the message to the push notification.</summary>
        /// <param name="builder">The builder.</param>
        /// <param name="title">The title to add to the push notification.</param>
        public static PushNotificationMessageBuilder WithTitle(this PushNotificationMessageBuilder builder, string title) {
            if (string.IsNullOrEmpty(title)) {
                throw new ArgumentException("You must define a title for the push notification.", nameof(title));
            }
            builder.Title = title;
            return builder;
        }

        /// <summary>Defines the body of the push notification.</summary>
        /// <param name="builder">The builder.</param>
        /// <param name="body">The body of the push notification.</param>
        public static PushNotificationMessageBuilder WithBody(this PushNotificationMessageBuilder builder, string body) {
            if (string.IsNullOrEmpty(body)) {
                throw new ArgumentException("You must define a body for the push notification.", nameof(body));
            }
            builder.Body = body;
            return builder;
        }

        /// <summary>Defines the data of the push notification. Data is optional.</summary>
        /// <param name="builder">The builder.</param>
        /// <param name="data">The data that will be sent to the push notification.</param>
        public static PushNotificationMessageBuilder WithData(this PushNotificationMessageBuilder builder, string data) {
            builder.Data = data;
            return builder;
        }

        /// <summary>Defines the data of the push notification. Data is optional.</summary>
        /// <param name="builder">The builder.</param>
        /// <param name="data">The data that will be sent to the push notification.</param>
        public static PushNotificationMessageBuilder WithData<TData>(this PushNotificationMessageBuilder builder, TData data) where TData : class {
            var dataJson = JsonSerializer.Serialize(data, JsonSerializerOptionDefaults.GetDefaultSettings());
            builder.Data = dataJson;
            return builder;
        }

        /// <summary>Defines the user that will receive the push notification.</summary>
        /// <param name="builder">The builder.</param>
        /// <param name="userTag">The Id of the user.</param>
        public static PushNotificationMessageBuilder ToUser(this PushNotificationMessageBuilder builder, string userTag) {
            if (string.IsNullOrEmpty(userTag)) {
                throw new ArgumentException("You must define the userId of the push notification.", nameof(userTag));
            }
            builder.UserTag = userTag;
            return builder;
        }

        /// <summary>Defines the user that will receive the push notification.</summary>
        /// <param name="builder">The builder.</param>
        /// <param name="deviceId">The Id of the device.</param>
        public static PushNotificationMessageBuilder ToDevice(this PushNotificationMessageBuilder builder, string deviceId) {
            if (string.IsNullOrEmpty(deviceId)) {
                throw new ArgumentException("You must define the userId of the push notification.", nameof(deviceId));
            }
            builder.UserTag = deviceId;
            return builder;
        }

        /// <summary>Defines the tags that will be sent to the push notification.</summary>
        /// <param name="builder">The builder.</param>
        /// <param name="tags">The tags to send.</param>
        public static PushNotificationMessageBuilder WithTags(this PushNotificationMessageBuilder builder, params string[] tags) {
            if (tags?.Length == 0) {
                throw new ArgumentException("You must set the tags to the push notification.", nameof(tags));
            }
            builder.Tags = tags;
            return builder;
        }

        /// <summary>Defines the type of the push notification.</summary>
        /// <param name="builder">the builder.</param>
        /// <param name="classification">The type of the push notification.</param>
        public static PushNotificationMessageBuilder WithClassification(this PushNotificationMessageBuilder builder, string classification) {
            builder.Classification = classification;
            return builder;
        }

        /// <summary>Returns the <see cref="PushNotificationMessage"/> instance made by the builder.</summary>
        /// <param name="builder">The builder.</param>
        public static PushNotificationMessage Build(this PushNotificationMessageBuilder builder) =>
            new(builder.Title, builder.Body, builder.Data, builder.UserTag, builder.Tags, builder.Classification);
    }
}
