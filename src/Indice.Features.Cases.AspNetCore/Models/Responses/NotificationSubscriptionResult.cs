namespace Indice.Features.Cases.Models.Responses
{
    /// <summary>
    /// The DTO for the Notification Subscription for a user.
    /// </summary>
    public class NotificationSubscriptionResult
    {
        /// <summary>
        /// Indicates if the user is subscribed to the current group.
        /// </summary>
        public bool Subscribed { get; set; }
    }
}