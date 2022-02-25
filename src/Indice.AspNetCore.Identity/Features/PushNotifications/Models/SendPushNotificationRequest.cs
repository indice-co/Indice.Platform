using System.Dynamic;

namespace Indice.AspNetCore.Identity.Api.Models
{
    /// <summary>
    /// Models a request when sending a push notification.
    /// </summary>
    public class SendPushNotificationRequest
    {
        /// <summary>
        /// The title to send.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// The body to send.
        /// </summary>
        public string Body { get; set; }
        /// <summary>
        /// Defines if push notification is sent to all registered user devices.
        /// </summary>
        public bool Broadcast { get; set; }
        /// <summary>
        /// The user identifier that correlates devices with users. This can be any identifier like user id, username, user email, customer code etc. Required when <see cref="Broadcast"/> has the value <i>false</i>.
        /// </summary>
        /// <remarks>Use this for unicast. Single user notification.</remarks>
        public string UserTag { get; set; }
        /// <summary>
        /// List of extra tags.
        /// </summary>
        public string[] Tags { get; set; }
        /// <summary>
        /// Notification data.
        /// </summary>
        public ExpandoObject Data { get; set; }
        /// <summary>
        /// Notification classification.
        /// </summary>
        public string Classification { get; set; }
    }
}
