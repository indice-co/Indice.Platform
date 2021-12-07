using System.Dynamic;

namespace Indice.AspNetCore.Identity.Api.Models
{
    /// <summary>
    /// Models a request when sending a push notification.
    /// </summary>
    public class SendPushNotificationRequest
    {
        /// <summary>
        /// The message to send.
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Defines if push notification is sent to all registered user devices.
        /// </summary>
        public bool Broadcast { get; set; }
        /// <summary>
        /// The user code. Required when <see cref="Broadcast"/> has the value <i>false</i>.
        /// </summary>
        public string UserCode { get; set; }
        /// <summary>
        /// Notification data.
        /// </summary>
        public ExpandoObject Data { get; set; }
        /// <summary>
        /// List of extra tags.
        /// </summary>
        public string[] Tags { get; set; }
        /// <summary>
        /// Notification classification.
        /// </summary>
        public string Classification { get; set; }
    }
}
