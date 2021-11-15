using System;

namespace Indice.Services
{
    /// <summary>
    /// The builder to construct an instance of <see cref="PushNotificationMessage"/>.
    /// </summary>
    public class PushNotificationMessageBuilder
    {
        /// <summary>
        /// The payload data that will be sent to the mobile client (not visible to the Push Notification Title or Message).
        /// If the data is null then only the token will be sent as data.
        /// </summary>
        public string Data { get; set; }
        /// <summary>
        /// The message of the push notification.
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// The OTP token that must be passed to the client.
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// The UserId to send the push notification.
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// The tags of the push notification.
        /// </summary>
        public string[] Tags { get; set; } = Array.Empty<string>();
        /// <summary>
        /// The type of the push notification.
        /// </summary>
        public string Classification { get; set; }
    }

    /// <summary>
    /// <see cref="PushNotificationMessageBuilder"/> extensions.
    /// </summary>
    public static class PushNotificationMessageBuilderExtensions
    {
        /// <summary>
        /// Defines the message to the Push Notification. 
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="message">The message to add to the push notification</param>
        /// <returns></returns>
        public static PushNotificationMessageBuilder WithMessage(this PushNotificationMessageBuilder builder, string message) {
            if (string.IsNullOrEmpty(message)) {
                throw new ArgumentException("You must define a message for the Push Notification.", nameof(message));
            }
            builder.Message = message;
            return builder;
        }

        /// <summary>
        /// Defines the data of the push notification. Data is optional.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="data">The data that will be sent to the push notification.</param>
        public static PushNotificationMessageBuilder WithData(this PushNotificationMessageBuilder builder, string data) {
            builder.Data = data;
            return builder;
        }

        /// <summary>
        /// Defines the otp token that will be sent to the push notification.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="token">The otp token.</param>
        public static PushNotificationMessageBuilder WithToken(this PushNotificationMessageBuilder builder, string token) {
            if (string.IsNullOrEmpty(token)) {
                throw new ArgumentException("You must define the otp token of the Push Notification.", nameof(token));
            }
            builder.Token = token;
            return builder;
        }

        /// <summary>
        /// Defines the user that will receive the push notification.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="userId">The Id of the user.</param>
        public static PushNotificationMessageBuilder To(this PushNotificationMessageBuilder builder, string userId) {
            if (string.IsNullOrEmpty(userId)) {
                throw new ArgumentException("You must define the userId of the Push Notification.", nameof(userId));
            }
            builder.UserId = userId;
            return builder;
        }

        /// <summary>
        /// Defines the tags that will be sent to the push notification.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="tags">The tags to send.</param>
        public static PushNotificationMessageBuilder WithTags(this PushNotificationMessageBuilder builder, params string[] tags) {
            if (tags?.Length == 0) {
                throw new ArgumentException("You must set the tags to the Push Notification.", nameof(tags));
            }
            builder.Tags = tags;
            return builder;
        }

        /// <summary>
        /// Defines the type of the push notification.
        /// </summary>
        /// <param name="builder">the builder.</param>
        /// <param name="classification">The type of the push notification.</param>
        public static PushNotificationMessageBuilder WithClassification(this PushNotificationMessageBuilder builder, string classification) {
            builder.Classification = classification;
            return builder;
        }

        /// <summary>
        /// Returns the <see cref="PushNotificationMessage"/> instance made by the builder.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static PushNotificationMessage Build(this PushNotificationMessageBuilder builder) =>
            new(builder.Data, builder.Message, builder.Token, builder.UserId, builder.Tags, builder.Classification);
    }
}