using System;

namespace Indice.Services
{
    /// <summary>
    /// Models the data that are sent in an push notification message.
    /// </summary>
    public class PushNotificationMessage
    {
        /// <summary>
        /// The payload data that will be sent to the mobile client (not visible to the Push Notification Title or Message).
        /// If the data is null then only the token will be sent as data.
        /// </summary>
        public string Data { get; }

        /// <summary>
        /// The message of the Push Notification.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The Otp token that must be passed to the client.
        /// </summary>
        public string Token { get; }

        /// <summary>
        /// The user identifier that correlates devices with users. This can be any identifier like user id, username, user email, customer code etc.
        /// </summary>
        public string UserTag { get; }

        /// <summary>
        /// The tags of the Push Notification.
        /// </summary>
        public string[] Tags { get; }

        /// <summary>
        /// The type of the Push Notification.
        /// </summary>
        public string Classification { get; }

        /// <summary>
        /// Constructs a <see cref="PushNotificationMessage"/>.
        /// </summary>
        /// <param name="data">The payload data that will be sent to the mobile client (not visible to the Push Notification Title or Message).  If the data is null then only the token will be sent as data.</param>
        /// <param name="message">The message of the Push Notification.</param>
        /// <param name="token">The Otp token that must be passed to the client.</param>
        /// <param name="userTag">The UserId to send the Push Notification.</param>
        /// <param name="tags">The tags of the Push Notification.</param>
        /// <param name="classification">The type of the Push Notification.</param>
        public PushNotificationMessage(string data, string message, string token, string userTag, string[] tags, string classification) {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Token = token ?? throw new ArgumentNullException(nameof(token));
            UserTag = userTag ?? throw new ArgumentNullException(nameof(userTag));
            Data = string.Format(data ?? "{0}", Token);
            Tags = tags ?? Array.Empty<string>();
            Classification = classification;
        }
    }
}