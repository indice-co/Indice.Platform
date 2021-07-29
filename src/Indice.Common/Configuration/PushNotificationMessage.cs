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
        /// The UserId to send the Push Notification.
        /// </summary>
        public string UserId { get; }

        /// <summary>
        /// The tags of the Push Notification.
        /// </summary>
        public string[] Tags { get; }

        /// <summary>
        /// Constructs a <see cref="PushNotificationMessage"/>.
        /// </summary>
        /// <param name="data">The payload data that will be sent to the mobile client (not visible to the Push Notification Title or Message).  If the data is null then only the token will be sent as data.</param>
        /// <param name="message">The message of the Push Notification.</param>
        /// <param name="token">The Otp token that must be passed to the client.</param>
        /// <param name="userId">The UserId to send the Push Notification.</param>
        /// <param name="tags">The tags of the Push Notification.</param>
        public PushNotificationMessage(string data, string message, string token, string userId, string[] tags) {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Token = token ?? throw new ArgumentNullException(nameof(token));
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
            Data = string.Format(data ?? "{0}", Token);
            Tags = tags ?? Array.Empty<string>();
        }
    }
}